using KubernetesController.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace KubernetesController
{
    public class CreateIngressForm : Form
    {
        private TextBox txtIngressName;
        private ComboBox cmbNamespace;
        private TextBox txtHost;
        private TextBox txtPath;
        private ComboBox cmbServiceName;
        private NumericUpDown nudServicePort;
        private Button btnCreate;
        private Button btnCancel;

        private List<KubernetesNamespaceDetails> availableNamespaces;
        private List<KubernetesServiceDetails> availableServices;
        private bool updatingServiceCombo;

        public string IngressName { get; private set; }
        public string NamespaceName { get; private set; }
        public string Host { get; private set; }
        public string PathValue { get; private set; }
        public string ServiceName { get; private set; }
        public string ServicePort { get; private set; }

        private class ServiceOption
        {
            public string NamespaceName { get; set; }
            public string ServiceName { get; set; }
            public int Port { get; set; }

            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(NamespaceName))
                    return ServiceName;

                return NamespaceName + "/" + ServiceName;
            }
        }

        public CreateIngressForm()
            : this(null, null)
        {
        }

        public CreateIngressForm(List<KubernetesNamespaceDetails> namespaces, List<KubernetesServiceDetails> services)
        {
            availableNamespaces = namespaces == null ? new List<KubernetesNamespaceDetails>() : namespaces;
            availableServices = services == null ? new List<KubernetesServiceDetails>() : services;

            InitializeForm();
            LoadNamespaceOptions();
            LoadServiceOptionsForSelectedNamespace();
        }

        private void InitializeForm()
        {
            this.Text = "Criar Ingress";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(455, 325);

            Label lblIngressName = CreateLabel("Nome do Ingress", 20, 22);
            txtIngressName = CreateTextBox(190, 18, 230, "test-ingress");

            Label lblNamespace = CreateLabel("Namespace", 20, 62);
            cmbNamespace = CreateComboBox(190, 58, 230, true);
            cmbNamespace.SelectedIndexChanged += cmbNamespace_SelectedIndexChanged;

            Label lblHost = CreateLabel("Host (opcional)", 20, 102);
            txtHost = CreateTextBox(190, 98, 230, "");

            Label lblPath = CreateLabel("Path", 20, 142);
            txtPath = CreateTextBox(190, 138, 230, "/");

            Label lblServiceName = CreateLabel("Service destino", 20, 182);
            cmbServiceName = CreateComboBox(190, 178, 230, false);
            cmbServiceName.SelectedIndexChanged += cmbServiceName_SelectedIndexChanged;

            Label lblServicePort = CreateLabel("Porta do Service", 20, 222);
            nudServicePort = new NumericUpDown();
            nudServicePort.Location = new Point(190, 218);
            nudServicePort.Size = new Size(230, 26);
            nudServicePort.Minimum = 1;
            nudServicePort.Maximum = 65535;
            nudServicePort.Value = 80;

            btnCreate = new Button();
            btnCreate.Text = "Criar";
            btnCreate.Location = new Point(190, 270);
            btnCreate.Size = new Size(110, 32);
            btnCreate.UseVisualStyleBackColor = true;
            btnCreate.Click += btnCreate_Click;

            btnCancel = new Button();
            btnCancel.Text = "Cancelar";
            btnCancel.Location = new Point(320, 270);
            btnCancel.Size = new Size(100, 32);
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblIngressName);
            this.Controls.Add(txtIngressName);
            this.Controls.Add(lblNamespace);
            this.Controls.Add(cmbNamespace);
            this.Controls.Add(lblHost);
            this.Controls.Add(txtHost);
            this.Controls.Add(lblPath);
            this.Controls.Add(txtPath);
            this.Controls.Add(lblServiceName);
            this.Controls.Add(cmbServiceName);
            this.Controls.Add(lblServicePort);
            this.Controls.Add(nudServicePort);
            this.Controls.Add(btnCreate);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnCreate;
            this.CancelButton = btnCancel;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.Size = new Size(165, 24);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private TextBox CreateTextBox(int x, int y, int width, string value)
        {
            TextBox textBox = new TextBox();
            textBox.Location = new Point(x, y);
            textBox.Size = new Size(width, 26);
            textBox.Text = value;
            return textBox;
        }

        private ComboBox CreateComboBox(int x, int y, int width, bool dropDownList)
        {
            ComboBox comboBox = new ComboBox();
            comboBox.Location = new Point(x, y);
            comboBox.Size = new Size(width, 26);
            comboBox.DropDownStyle = dropDownList ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;

            // A ordem é importante para evitar erro no WinForms:
            // primeiro define-se a fonte de autocomplete e só depois o modo.
            comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
            comboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;

            return comboBox;
        }

        private void LoadNamespaceOptions()
        {
            List<string> namespaces = new List<string>();

            if (availableNamespaces != null)
            {
                namespaces.AddRange(
                    availableNamespaces
                        .Where(n => n != null && !string.IsNullOrWhiteSpace(n.Name))
                        .Select(n => n.Name.Trim())
                );
            }

            if (availableServices != null)
            {
                namespaces.AddRange(
                    availableServices
                        .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Namespace))
                        .Select(s => s.Namespace.Trim())
                );
            }

            namespaces = namespaces
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => string.Equals(n, "default", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(n => n)
                .ToList();

            if (!namespaces.Any(n => string.Equals(n, "default", StringComparison.OrdinalIgnoreCase)))
                namespaces.Insert(0, "default");

            cmbNamespace.Items.Clear();

            foreach (string namespaceName in namespaces)
                cmbNamespace.Items.Add(namespaceName);

            int defaultIndex = FindComboBoxItemIndex(cmbNamespace, "default");
            cmbNamespace.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
        }

        private void LoadServiceOptionsForSelectedNamespace()
        {
            if (cmbServiceName == null)
                return;

            updatingServiceCombo = true;

            try
            {
                string namespaceName = GetComboBoxText(cmbNamespace);
                string previousText = GetComboBoxText(cmbServiceName);

                List<ServiceOption> services = BuildServiceOptions(namespaceName);

                cmbServiceName.Items.Clear();

                foreach (ServiceOption service in services)
                    cmbServiceName.Items.Add(service);

                int previousIndex = FindServiceOptionIndex(services, previousText);
                int namespaceIndex = FindFirstServiceOptionIndexForNamespace(services, namespaceName);

                if (previousIndex >= 0)
                    cmbServiceName.SelectedIndex = previousIndex;
                else if (namespaceIndex >= 0)
                    cmbServiceName.SelectedIndex = namespaceIndex;
                else if (services.Count > 0)
                    cmbServiceName.SelectedIndex = 0;
                else
                    cmbServiceName.Text = "test-service";
            }
            finally
            {
                updatingServiceCombo = false;
            }

            UpdatePortFromSelectedService();
        }

        private List<ServiceOption> BuildServiceOptions(string selectedNamespace)
        {
            List<ServiceOption> result = new List<ServiceOption>();

            if (availableServices == null)
                return result;

            foreach (KubernetesServiceDetails service in availableServices)
            {
                if (service == null || string.IsNullOrWhiteSpace(service.Name))
                    continue;

                ServiceOption option = new ServiceOption();
                option.NamespaceName = string.IsNullOrWhiteSpace(service.Namespace) ? "default" : service.Namespace.Trim();
                option.ServiceName = service.Name.Trim();

                int port;
                option.Port = TryGetServicePort(service, out port) ? port : 80;

                bool exists = result.Any(s =>
                    string.Equals(s.NamespaceName, option.NamespaceName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.ServiceName, option.ServiceName, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                    result.Add(option);
            }

            return result
                .OrderBy(s => string.Equals(s.NamespaceName, selectedNamespace, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(s => s.NamespaceName)
                .ThenBy(s => s.ServiceName)
                .ToList();
        }

        private void UpdatePortFromSelectedService()
        {
            if (nudServicePort == null)
                return;

            ServiceOption selectedOption = cmbServiceName == null ? null : cmbServiceName.SelectedItem as ServiceOption;

            if (selectedOption != null)
            {
                SetNumericPortValue(selectedOption.Port);
                return;
            }

            if (availableServices == null)
                return;

            string namespaceName = GetComboBoxText(cmbNamespace);
            string serviceName = GetComboBoxText(cmbServiceName);

            if (string.IsNullOrWhiteSpace(serviceName))
                return;

            if (serviceName.Contains("/"))
            {
                string[] parts = serviceName.Split(new char[] { '/' }, 2);
                if (parts.Length == 2)
                {
                    namespaceName = parts[0].Trim();
                    serviceName = parts[1].Trim();
                }
            }

            KubernetesServiceDetails selectedService = availableServices.FirstOrDefault(s =>
                s != null &&
                string.Equals(s.Namespace, namespaceName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Name, serviceName, StringComparison.OrdinalIgnoreCase));

            if (selectedService == null || selectedService.Ports == null || selectedService.Ports.Count == 0)
                return;

            int port;
            if (TryGetServicePort(selectedService, out port))
                SetNumericPortValue(port);
        }

        private bool TryGetServicePort(KubernetesServiceDetails service, out int port)
        {
            port = 80;

            if (service == null || service.Ports == null)
                return false;

            foreach (KubernetesServicePort servicePort in service.Ports)
            {
                if (servicePort == null || string.IsNullOrWhiteSpace(servicePort.Port))
                    continue;

                if (int.TryParse(servicePort.Port.Trim(), out port))
                    return true;
            }

            return false;
        }

        private void SetNumericPortValue(int port)
        {
            if (nudServicePort == null)
                return;

            if (port < nudServicePort.Minimum)
                port = Convert.ToInt32(nudServicePort.Minimum);

            if (port > nudServicePort.Maximum)
                port = Convert.ToInt32(nudServicePort.Maximum);

            nudServicePort.Value = port;
        }

        private int FindComboBoxItemIndex(ComboBox comboBox, string value)
        {
            if (comboBox == null || string.IsNullOrWhiteSpace(value))
                return -1;

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                string itemText = Convert.ToString(comboBox.Items[i]);

                if (string.Equals(itemText, value, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private int FindServiceOptionIndex(List<ServiceOption> options, string previousText)
        {
            if (options == null || string.IsNullOrWhiteSpace(previousText))
                return -1;

            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i].ToString(), previousText.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(options[i].ServiceName, previousText.Trim(), StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private int FindFirstServiceOptionIndexForNamespace(List<ServiceOption> options, string namespaceName)
        {
            if (options == null || string.IsNullOrWhiteSpace(namespaceName))
                return -1;

            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i].NamespaceName, namespaceName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private string GetComboBoxText(ComboBox comboBox)
        {
            if (comboBox == null)
                return "";

            if (comboBox.SelectedItem != null)
                return Convert.ToString(comboBox.SelectedItem).Trim();

            return comboBox.Text.Trim();
        }

        private string GetSelectedServiceName()
        {
            ServiceOption option = cmbServiceName == null ? null : cmbServiceName.SelectedItem as ServiceOption;

            if (option != null)
                return option.ServiceName;

            string serviceName = GetComboBoxText(cmbServiceName);

            if (serviceName.Contains("/"))
            {
                string[] parts = serviceName.Split(new char[] { '/' }, 2);
                if (parts.Length == 2)
                    return parts[1].Trim();
            }

            return serviceName;
        }

        private string GetSelectedServiceNamespace()
        {
            ServiceOption option = cmbServiceName == null ? null : cmbServiceName.SelectedItem as ServiceOption;

            if (option != null && !string.IsNullOrWhiteSpace(option.NamespaceName))
                return option.NamespaceName;

            string serviceName = GetComboBoxText(cmbServiceName);

            if (serviceName.Contains("/"))
            {
                string[] parts = serviceName.Split(new char[] { '/' }, 2);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    return parts[0].Trim();
            }

            return GetComboBoxText(cmbNamespace);
        }

        private void SelectNamespace(string namespaceName)
        {
            if (cmbNamespace == null || string.IsNullOrWhiteSpace(namespaceName))
                return;

            int index = FindComboBoxItemIndex(cmbNamespace, namespaceName);

            if (index >= 0)
                cmbNamespace.SelectedIndex = index;
        }

        private void cmbNamespace_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (updatingServiceCombo)
                return;

            LoadServiceOptionsForSelectedNamespace();
        }

        private void cmbServiceName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updatingServiceCombo)
            {
                ServiceOption selectedOption = cmbServiceName.SelectedItem as ServiceOption;

                if (selectedOption != null)
                {
                    updatingServiceCombo = true;
                    SelectNamespace(selectedOption.NamespaceName);
                    updatingServiceCombo = false;
                }
            }

            UpdatePortFromSelectedService();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            string ingressName = txtIngressName.Text.Trim();
            string namespaceName = GetSelectedServiceNamespace();
            string host = txtHost.Text.Trim();
            string path = txtPath.Text.Trim();
            string serviceName = GetSelectedServiceName();
            string servicePort = Convert.ToInt32(nudServicePort.Value).ToString();

            if (string.IsNullOrWhiteSpace(ingressName))
            {
                MessageBox.Show("Indica o nome do ingress.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIngressName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
                namespaceName = "default";

            if (string.IsNullOrWhiteSpace(path))
                path = "/";

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                MessageBox.Show("Indica o service de destino.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbServiceName.Focus();
                return;
            }

            IngressName = ingressName;
            NamespaceName = namespaceName;
            Host = host;
            PathValue = path;
            ServiceName = serviceName;
            ServicePort = servicePort;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
