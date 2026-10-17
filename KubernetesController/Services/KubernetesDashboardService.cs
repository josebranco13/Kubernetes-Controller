using KubernetesController.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KubernetesController.Services
{
    public class KubernetesDashboardService
    {
        private readonly KubernetesApiClient api;

        public KubernetesDashboardService(KubernetesApiClient api)
        {
            this.api = api;
        }

        public async Task<KubernetesDashboardSummary> GetDashboardSummaryAsync()
        {
            KubernetesDashboardSummary summary = new KubernetesDashboardSummary();

            string nodesJson = await api.GetAsync("/api/v1/nodes");
            string namespacesJson = await api.GetAsync("/api/v1/namespaces");
            string podsJson = await api.GetAsync("/api/v1/pods");
            string deploymentsJson = await api.GetAsync("/apis/apps/v1/deployments");
            string servicesJson = await api.GetAsync("/api/v1/services");
            string ingressesJson = await api.GetAsync("/apis/networking.k8s.io/v1/ingresses");

            ParseNodes(nodesJson, summary);
            ParseNamespaces(namespacesJson, summary);
            ParsePods(podsJson, summary);
            ParseDeployments(deploymentsJson, summary);
            ParseServices(servicesJson, summary);
            ParseIngresses(ingressesJson, summary);

            if (summary.ImagesCount.Count > 0)
            {
                summary.TopImage = summary.ImagesCount
                    .OrderByDescending(i => i.Value)
                    .ThenBy(i => i.Key)
                    .First()
                    .Key;
            }

            summary.TotalMemoryGiB = Math.Round(summary.TotalMemoryGiB, 2);
            return summary;
        }

        private void ParseNodes(string json, KubernetesDashboardSummary summary)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("items", out JsonElement items))
                    return;

                summary.TotalNodes = items.GetArrayLength();

                foreach (JsonElement node in items.EnumerateArray())
                {
                    JsonElement metadata = node.GetProperty("metadata");
                    JsonElement status = node.GetProperty("status");
                    JsonElement capacity = status.GetProperty("capacity");

                    string nodeName = GetStringValue(metadata, "name");
                    bool ready = IsNodeReady(status);

                    if (ready)
                        summary.ReadyNodes++;
                    else
                        summary.NotReadyNodes++;

                    AddCount(summary.NodesStatus, ready ? "Pronto" : "Não Pronto");

                    int cpu = GetCpuValue(capacity);
                    double memoryGiB = GetMemoryGiB(capacity);
                    int podCapacity = GetIntValue(capacity, "pods");

                    summary.TotalCpu += cpu;
                    summary.TotalMemoryGiB += memoryGiB;
                    summary.TotalPodCapacity += podCapacity;

                    summary.CpuByNode[nodeName] = cpu;
                    summary.MemoryByNode[nodeName] = memoryGiB;

                    if (status.TryGetProperty("nodeInfo", out JsonElement nodeInfo))
                    {
                        string kubeletVersion = GetStringValue(nodeInfo, "kubeletVersion");

                        if (string.IsNullOrWhiteSpace(kubeletVersion))
                            kubeletVersion = "N/A";

                        AddCount(summary.KubeletVersions, kubeletVersion);
                    }

                    if (IsControlPlane(metadata))
                        summary.MasterIp = GetInternalIp(status);

                    if (status.TryGetProperty("images", out JsonElement images))
                        ParseImages(images, summary);
                }
            }
        }

        private void ParseNamespaces(string json, KubernetesDashboardSummary summary)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.TryGetProperty("items", out JsonElement items))
                    summary.TotalNamespaces = items.GetArrayLength();
            }
        }

        private void ParsePods(string json, KubernetesDashboardSummary summary)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("items", out JsonElement items))
                    return;

                summary.TotalPods = items.GetArrayLength();

                foreach (JsonElement pod in items.EnumerateArray())
                {
                    JsonElement metadata = pod.GetProperty("metadata");
                    string ns = GetStringValue(metadata, "namespace");

                    if (string.IsNullOrWhiteSpace(ns))
                        ns = "default";

                    AddCount(summary.PodsByNamespace, ns);

                    string phase = "Unknown";
                    if (pod.TryGetProperty("status", out JsonElement status))
                        phase = GetStringValue(status, "phase");

                    if (string.IsNullOrWhiteSpace(phase))
                        phase = "Unknown";

                    if (phase == "Running")
                        summary.RunningPods++;

                    AddCount(summary.PodStatus, phase);
                }
            }
        }

        private void ParseDeployments(string json, KubernetesDashboardSummary summary)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("items", out JsonElement items))
                    return;

                summary.TotalDeployments = items.GetArrayLength();

                foreach (JsonElement deployment in items.EnumerateArray())
                {
                    JsonElement metadata = deployment.GetProperty("metadata");
                    string ns = GetStringValue(metadata, "namespace");

                    if (string.IsNullOrWhiteSpace(ns))
                        ns = "default";

                    AddCount(summary.DeploymentsByNamespace, ns);

                    int availableReplicas = 0;
                    JsonElement status;

                    if (deployment.TryGetProperty("status", out status))
                        availableReplicas = GetIntValue(status, "availableReplicas");

                    if (availableReplicas > 0)
                        summary.ActiveDeployments++;

                    AddCount(summary.DeploymentStatus, GetDeploymentStatus(deployment));
                }
            }
        }

        private string GetDeploymentStatus(JsonElement deployment)
        {
            int desiredReplicas = 1;
            int availableReplicas = 0;
            int readyReplicas = 0;
            int unavailableReplicas = 0;

            if (deployment.TryGetProperty("spec", out JsonElement spec))
                desiredReplicas = GetIntValue(spec, "replicas");

            if (deployment.TryGetProperty("status", out JsonElement status))
            {
                availableReplicas = GetIntValue(status, "availableReplicas");
                readyReplicas = GetIntValue(status, "readyReplicas");
                unavailableReplicas = GetIntValue(status, "unavailableReplicas");
            }

            if (desiredReplicas == 0)
                return "Escalado a 0";

            if (availableReplicas >= desiredReplicas && readyReplicas >= desiredReplicas)
                return "Disponível";

            if (availableReplicas > 0 || readyReplicas > 0)
                return "Parcial";

            if (unavailableReplicas > 0)
                return "Indisponível";

            return "Sem réplicas disponíveis";
        }

        private void ParseServices(string json, KubernetesDashboardSummary summary)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("items", out JsonElement items))
                    return;

                summary.TotalServices = items.GetArrayLength();

                foreach (JsonElement service in items.EnumerateArray())
                {
                    JsonElement metadata = service.GetProperty("metadata");
                    string ns = GetStringValue(metadata, "namespace");

                    if (string.IsNullOrWhiteSpace(ns))
                        ns = "default";

                    AddCount(summary.ServicesByNamespace, ns);
                }
            }
        }

        private void ParseIngresses(string json, KubernetesDashboardSummary summary)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.TryGetProperty("items", out JsonElement items))
                    summary.TotalIngresses = items.GetArrayLength();
            }
        }

        private void ParseImages(JsonElement images, KubernetesDashboardSummary summary)
        {
            foreach (JsonElement image in images.EnumerateArray())
            {
                if (!image.TryGetProperty("names", out JsonElement names))
                    continue;

                string selectedName = "";

                foreach (JsonElement name in names.EnumerateArray())
                {
                    string current = name.GetString() ?? "";

                    if (string.IsNullOrWhiteSpace(current))
                        continue;

                    // Dá preferência ao nome normal da imagem e evita usar o digest sha256,
                    // porque esse formato é demasiado longo para a dashboard.
                    if (current.IndexOf("@sha256:", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        selectedName = current;
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(selectedName))
                        selectedName = current;
                }

                selectedName = NormalizeImageNameForDashboard(selectedName);

                if (!string.IsNullOrWhiteSpace(selectedName))
                    AddCount(summary.ImagesCount, selectedName);
            }
        }

        private string NormalizeImageNameForDashboard(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
                return "N/A";

            string clean = imageName.Trim();

            int digestIndex = clean.IndexOf("@sha256:", StringComparison.OrdinalIgnoreCase);
            if (digestIndex >= 0)
                clean = clean.Substring(0, digestIndex);

            if (clean.StartsWith("docker.io/library/", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring("docker.io/library/".Length);
            else if (clean.StartsWith("docker.io/", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring("docker.io/".Length);
            else if (clean.StartsWith("index.docker.io/library/", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring("index.docker.io/library/".Length);
            else if (clean.StartsWith("index.docker.io/", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring("index.docker.io/".Length);

            return clean;
        }

        private bool IsNodeReady(JsonElement status)
        {
            if (!status.TryGetProperty("conditions", out JsonElement conditions))
                return false;

            foreach (JsonElement condition in conditions.EnumerateArray())
            {
                string type = GetStringValue(condition, "type");
                string value = GetStringValue(condition, "status");

                if (type == "Ready")
                    return value == "True";
            }

            return false;
        }

        private bool IsControlPlane(JsonElement metadata)
        {
            if (!metadata.TryGetProperty("labels", out JsonElement labels))
                return false;

            return labels.TryGetProperty("node-role.kubernetes.io/control-plane", out _) ||
                   labels.TryGetProperty("node-role.kubernetes.io/master", out _);
        }

        private string GetInternalIp(JsonElement status)
        {
            if (!status.TryGetProperty("addresses", out JsonElement addresses))
                return "N/A";

            foreach (JsonElement address in addresses.EnumerateArray())
            {
                string type = GetStringValue(address, "type");

                if (type == "InternalIP")
                    return GetStringValue(address, "address");
            }

            return "N/A";
        }

        private int GetCpuValue(JsonElement capacity)
        {
            if (!capacity.TryGetProperty("cpu", out JsonElement cpuElement))
                return 0;

            string value = cpuElement.GetString() ?? "0";

            if (value.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Replace("m", "").Trim();
                if (double.TryParse(value, out double milliCpu))
                    return Convert.ToInt32(Math.Ceiling(milliCpu / 1000.0));
            }

            if (int.TryParse(value, out int result))
                return result;

            return 0;
        }

        private double GetMemoryGiB(JsonElement capacity)
        {
            if (!capacity.TryGetProperty("memory", out JsonElement memoryElement))
                return 0;

            // O Kubernetes pode devolver memória como 3974692Ki, 3881Mi, 3Gi, etc.
            // A versão anterior só removia "Ki" e, em alguns casos, acabava por mostrar 0 GiB.
            return Math.Round(ParseKubernetesQuantityToGiB(memoryElement.ToString()), 2);
        }

        private double ParseKubernetesQuantityToGiB(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            string clean = value.Trim();
            double multiplierToGiB = 1.0 / 1024.0 / 1024.0 / 1024.0;

            if (clean.EndsWith("Ki", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 2).Trim();
                multiplierToGiB = 1.0 / 1024.0 / 1024.0;
            }
            else if (clean.EndsWith("Mi", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 2).Trim();
                multiplierToGiB = 1.0 / 1024.0;
            }
            else if (clean.EndsWith("Gi", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 2).Trim();
                multiplierToGiB = 1.0;
            }
            else if (clean.EndsWith("Ti", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 2).Trim();
                multiplierToGiB = 1024.0;
            }
            else if (clean.EndsWith("Pi", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 2).Trim();
                multiplierToGiB = 1024.0 * 1024.0;
            }
            else if (clean.EndsWith("Ei", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 2).Trim();
                multiplierToGiB = 1024.0 * 1024.0 * 1024.0;
            }
            else if (clean.EndsWith("K", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 1).Trim();
                multiplierToGiB = 1000.0 / 1024.0 / 1024.0 / 1024.0;
            }
            else if (clean.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 1).Trim();
                multiplierToGiB = 1000.0 * 1000.0 / 1024.0 / 1024.0 / 1024.0;
            }
            else if (clean.EndsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 1).Trim();
                multiplierToGiB = 1000.0 * 1000.0 * 1000.0 / 1024.0 / 1024.0 / 1024.0;
            }

            double number;
            if (!double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                // Fallback para máquinas com cultura pt-PT, caso venha algum valor com vírgula.
                if (!double.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out number))
                    return 0;
            }

            return number * multiplierToGiB;
        }

        private int GetIntValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
                return 0;

            int result;

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out result))
                return result;

            if (property.ValueKind == JsonValueKind.String)
            {
                string value = property.GetString() ?? "0";

                if (int.TryParse(value, out result))
                    return result;
            }

            return 0;
        }

        private string GetStringValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
                return "";

            return property.GetString() ?? "";
        }

        private void AddCount(Dictionary<string, int> dictionary, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                key = "N/A";

            if (!dictionary.ContainsKey(key))
                dictionary[key] = 0;

            dictionary[key]++;
        }
    }
}
