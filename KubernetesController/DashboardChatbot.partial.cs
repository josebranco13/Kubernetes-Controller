using KubernetesController.Models;
using KubernetesController.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KubernetesController
{
    public partial class DashboardForm
    {
        private const string ChatbotModelName = "llama3.2:1b";

        private TabPage tabChatBot;
        private Panel pnlChatbotContent;
        private Label lblChatbotTitle;
        private Label lblChatbotStatus;
        private Label lblChatbotUrl;
        private TextBox txtChatbotUrl;
        private Button btnCheckChatbot;
        private RichTextBox rtbChatbotConversation;
        private TextBox txtChatbotQuestion;
        private Button btnSendChatbot;
        private OllamaChatbotService chatbotService;
        private bool chatbotCheckInProgress;

        private void ConfigureChatbotTabControls()
        {
            if (tabChatBot == null)
            {
                tabChatBot = new TabPage();
                tabChatBot.Name = "tabChatBot";
                tabChatBot.Text = "ChatBot";
                tabChatBot.Padding = new Padding(3);
                tabChatBot.UseVisualStyleBackColor = true;
                tabChatBot.TabIndex = tabKubernetesController.TabPages.Count;
                tabKubernetesController.Controls.Add(tabChatBot);
            }

            if (pnlChatbotContent != null)
                return;

            tabChatBot.Controls.Clear();

            pnlChatbotContent = new Panel();
            pnlChatbotContent.Name = "pnlChatbotContent";
            pnlChatbotContent.Dock = DockStyle.Fill;
            pnlChatbotContent.AutoScroll = true;
            tabChatBot.Controls.Add(pnlChatbotContent);

            lblChatbotTitle = new Label();
            lblChatbotTitle.Name = "lblChatbotTitle";
            lblChatbotTitle.Text = "ChatBot Kubernetes";
            lblChatbotTitle.Font = new Font(lblChatbotTitle.Font.FontFamily, 14, FontStyle.Bold);
            lblChatbotTitle.AutoSize = true;
            pnlChatbotContent.Controls.Add(lblChatbotTitle);

            lblChatbotStatus = new Label();
            lblChatbotStatus.Name = "lblChatbotStatus";
            lblChatbotStatus.Text = "Estado: ainda não verificado.";
            lblChatbotStatus.AutoSize = false;
            lblChatbotStatus.TextAlign = ContentAlignment.MiddleLeft;
            pnlChatbotContent.Controls.Add(lblChatbotStatus);

            lblChatbotUrl = new Label();
            lblChatbotUrl.Name = "lblChatbotUrl";
            lblChatbotUrl.Text = "URL do ChatBot:";
            lblChatbotUrl.AutoSize = true;
            pnlChatbotContent.Controls.Add(lblChatbotUrl);

            txtChatbotUrl = new TextBox();
            txtChatbotUrl.Name = "txtChatbotUrl";
            txtChatbotUrl.Text = BuildDefaultChatbotBaseUrl();
            pnlChatbotContent.Controls.Add(txtChatbotUrl);

            btnCheckChatbot = new Button();
            btnCheckChatbot.Name = "btnCheckChatbot";
            btnCheckChatbot.Text = "Verificar";
            btnCheckChatbot.UseVisualStyleBackColor = true;
            btnCheckChatbot.Click += new EventHandler(btnCheckChatbot_Click);
            pnlChatbotContent.Controls.Add(btnCheckChatbot);

            rtbChatbotConversation = new RichTextBox();
            rtbChatbotConversation.Name = "rtbChatbotConversation";
            rtbChatbotConversation.ReadOnly = true;
            rtbChatbotConversation.BackColor = Color.White;
            rtbChatbotConversation.BorderStyle = BorderStyle.FixedSingle;
            rtbChatbotConversation.Text = "ChatBot pronto para ser usado. Primeiro verifica se o serviço Ollama da VM está acessível.\n";
            pnlChatbotContent.Controls.Add(rtbChatbotConversation);

            txtChatbotQuestion = new TextBox();
            txtChatbotQuestion.Name = "txtChatbotQuestion";
            txtChatbotQuestion.Multiline = true;
            txtChatbotQuestion.ScrollBars = ScrollBars.Vertical;
            txtChatbotQuestion.KeyDown += new KeyEventHandler(txtChatbotQuestion_KeyDown);
            pnlChatbotContent.Controls.Add(txtChatbotQuestion);

            btnSendChatbot = new Button();
            btnSendChatbot.Name = "btnSendChatbot";
            btnSendChatbot.Text = "Enviar";
            btnSendChatbot.UseVisualStyleBackColor = true;
            btnSendChatbot.Click += new EventHandler(btnSendChatbot_Click);
            pnlChatbotContent.Controls.Add(btnSendChatbot);

            ArrangeChatbotLayout();
        }

        private void ArrangeChatbotLayout()
        {
            if (pnlChatbotContent == null)
                return;

            int margin = 24;
            int gap = 12;
            int contentWidth = Math.Max(780, pnlChatbotContent.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - (margin * 2));
            int contentHeight = Math.Max(500, pnlChatbotContent.ClientSize.Height - (margin * 2));

            lblChatbotTitle.Location = new Point(margin, 18);
            lblChatbotTitle.Size = new Size(contentWidth, 30);

            lblChatbotStatus.Location = new Point(margin, 55);
            lblChatbotStatus.Size = new Size(contentWidth, 28);

            lblChatbotUrl.Location = new Point(margin, 98);

            int urlTop = 92;
            int buttonWidth = 120;
            txtChatbotUrl.Location = new Point(margin + 110, urlTop);
            txtChatbotUrl.Size = new Size(contentWidth - 110 - buttonWidth - gap, 30);

            btnCheckChatbot.Location = new Point(margin + contentWidth - buttonWidth, urlTop - 1);
            btnCheckChatbot.Size = new Size(buttonWidth, 32);

            int historyTop = 140;
            int inputHeight = 76;
            int sendWidth = 120;
            int inputTop = margin + contentHeight - inputHeight;

            if (inputTop < historyTop + 180)
                inputTop = historyTop + 180;

            rtbChatbotConversation.Location = new Point(margin, historyTop);
            rtbChatbotConversation.Size = new Size(contentWidth, inputTop - historyTop - gap);

            txtChatbotQuestion.Location = new Point(margin, inputTop);
            txtChatbotQuestion.Size = new Size(contentWidth - sendWidth - gap, inputHeight);

            btnSendChatbot.Location = new Point(margin + contentWidth - sendWidth, inputTop);
            btnSendChatbot.Size = new Size(sendWidth, inputHeight);

            pnlChatbotContent.AutoScrollMinSize = new Size(contentWidth + (margin * 2), btnSendChatbot.Bottom + margin);
        }

        private string BuildDefaultChatbotBaseUrl()
        {
            // Acesso automático à máquina do ChatBot:
            // a aplicação aproveita o IP/host usado para ligar ao K3s e troca a porta para 11434,
            // que é a porta padrão do Ollama.
            //
            // Exemplo:
            // https://192.168.1.50:6443  ->  http://192.168.1.50:11434
            //
            // Se a app estiver ligada localmente ou por kubectl proxy,
            // usa por omissão http://localhost:11434.
            try
            {
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    Uri uri = new Uri(baseUrl);
                    string host = uri.Host;

                    if (!string.IsNullOrWhiteSpace(host))
                        return "http://" + host + ":11434";
                }
            }
            catch
            {
                // Em caso de URL inválida, usa o endereço local por omissão.
            }

            return "http://localhost:11434";
        }

        private OllamaChatbotService GetChatbotService()
        {
            string url = txtChatbotUrl == null ? BuildDefaultChatbotBaseUrl() : txtChatbotUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
                url = BuildDefaultChatbotBaseUrl();

            if (chatbotService == null || !string.Equals(chatbotService.BaseUrl, url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                chatbotService = new OllamaChatbotService(url, ChatbotModelName);

            return chatbotService;
        }

        private async Task CheckChatbotAccessAsync(bool userTriggered)
        {
            if (chatbotCheckInProgress || lblChatbotStatus == null)
                return;

            chatbotCheckInProgress = true;

            try
            {
                if (btnCheckChatbot != null)
                    btnCheckChatbot.Enabled = false;

                lblChatbotStatus.Text = "Estado: a verificar acesso ao ChatBot na VM...";

                OllamaChatbotService service = GetChatbotService();
                bool available = await service.CheckAvailabilityAsync();

                if (available)
                {
                    lblChatbotStatus.Text = "Estado: acessível em " + service.BaseUrl + " | Modelo: " + service.Model;

                    if (userTriggered)
                        AppendChatbotSystemMessage("Ligação ao ChatBot confirmada.");
                }
                else
                {
                    lblChatbotStatus.Text = "Estado: não foi possível aceder ao ChatBot em " + service.BaseUrl;

                    if (userTriggered)
                        AppendChatbotSystemMessage("Não foi possível aceder ao ChatBot. Confirma se o Ollama está ativo na VM.");
                }
            }
            catch
            {
                OllamaChatbotService service = GetChatbotService();
                lblChatbotStatus.Text = "Estado: não foi possível aceder ao ChatBot em " + service.BaseUrl;

                if (userTriggered)
                    AppendChatbotSystemMessage("Não foi possível aceder ao ChatBot. Confirma se o Ollama está ativo e acessível na porta 11434.");
            }
            finally
            {
                if (btnCheckChatbot != null)
                    btnCheckChatbot.Enabled = true;

                chatbotCheckInProgress = false;
            }
        }

        private async void btnCheckChatbot_Click(object sender, EventArgs e)
        {
            await CheckChatbotAccessAsync(true);
        }

        private async void btnSendChatbot_Click(object sender, EventArgs e)
        {
            await SendChatbotQuestionAsync();
        }

        private async void txtChatbotQuestion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendChatbotQuestionAsync();
            }
        }

        private async Task SendChatbotQuestionAsync()
        {
            if (txtChatbotQuestion == null || string.IsNullOrWhiteSpace(txtChatbotQuestion.Text))
                return;

            string question = txtChatbotQuestion.Text.Trim();
            txtChatbotQuestion.Clear();

            AppendChatbotMessage("Utilizador", question);

            // Resposta local para saudações simples.
            // Assim o utilizador recebe uma resposta rápida e não é necessário chamar o modelo Ollama.
            if (IsGreetingOnly(question))
            {
                AppendChatbotMessage("ChatBot", "Olá, em que posso ajudar?");
                return;
            }

            try
            {
                if (btnSendChatbot != null)
                    btnSendChatbot.Enabled = false;

                lblChatbotStatus.Text = "Estado: a enviar pergunta ao ChatBot...";

                OllamaChatbotService service = GetChatbotService();
                string prompt = BuildChatbotPrompt(question);
                string answer = await service.AskAsync(prompt);

                lblChatbotStatus.Text = "Estado: acessível em " + service.BaseUrl + " | Modelo: " + service.Model;
                AppendChatbotMessage("ChatBot", answer);
            }
            catch
            {
                OllamaChatbotService service = GetChatbotService();
                lblChatbotStatus.Text = "Estado: não foi possível aceder ao ChatBot em " + service.BaseUrl;
                AppendChatbotSystemMessage("Não foi possível obter resposta do ChatBot. Confirma se o Ollama está ativo na VM e se o modelo " + ChatbotModelName + " está instalado.");
            }
            finally
            {
                if (btnSendChatbot != null)
                    btnSendChatbot.Enabled = true;
            }
        }

        private bool IsGreetingOnly(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return false;

            string clean = question.Trim().ToLowerInvariant();

            clean = clean.Replace("!", "");
            clean = clean.Replace(".", "");
            clean = clean.Replace(",", "");
            clean = clean.Replace("?", "");
            clean = clean.Replace(";", "");
            clean = clean.Replace(":", "");

            return clean == "olá"
                || clean == "ola"
                || clean == "bom dia"
                || clean == "boa tarde"
                || clean == "boa noite"
                || clean == "hey"
                || clean == "boas"
                || clean == "viva";
        }

        private string BuildChatbotPrompt(string question)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("És o assistente oficial da aplicação KubernetesController.");
            builder.AppendLine();
            builder.AppendLine("A aplicação KubernetesController é uma aplicação C# WinForms que permite ao utilizador consultar e gerir recursos de um cluster Kubernetes/K3s através de uma interface gráfica.");
            builder.AppendLine();
            builder.AppendLine("Deves responder sempre em português de Portugal.");
            builder.AppendLine("Deves responder de forma simples, curta e clara, adequada a um utilizador iniciante.");
            builder.AppendLine();
            builder.AppendLine("Âmbito permitido:");
            builder.AppendLine("- explicar o que são Nodes, Namespaces, Pods, Deployments, Services e Ingresses;");
            builder.AppendLine("- explicar como consultar detalhes de recursos na aplicação;");
            builder.AppendLine("- explicar como criar Namespaces, Pods, Deployments, Services e Ingresses na aplicação;");
            builder.AppendLine("- explicar como apagar Namespaces, Pods, Deployments, Services e Ingresses na aplicação;");
            builder.AppendLine("- explicar como exportar recursos em YAML ou JSON;");
            builder.AppendLine("- explicar que a aplicação não exporta em XML;");
            builder.AppendLine("- interpretar o estado dos recursos mostrados pela aplicação;");
            builder.AppendLine("- explicar erros simples relacionados com Kubernetes, K3s, Pods, Deployments, Services, Ingresses e ligação ao cluster;");
            builder.AppendLine("- ajudar o utilizador a navegar nas abas Dashboard, Nodes, Namespaces, Pods, Deployments, Services, Ingresses e ChatBot.");
            builder.AppendLine();
            builder.AppendLine("Regras obrigatórias:");
            builder.AppendLine("- se a pergunta não estiver relacionada com a aplicação KubernetesController, com Kubernetes/K3s ou com os recursos apresentados na aplicação, responde apenas: \"Só posso ajudar com questões relacionadas com a aplicação KubernetesController e com os recursos Kubernetes apresentados na aplicação.\"");
            builder.AppendLine("- se o utilizador perguntar como exportar XML, explica que a aplicação não tem exportação XML e que apenas permite exportar em YAML ou JSON;");
            builder.AppendLine("- não inventes funcionalidades que a aplicação não tem;");
            builder.AppendLine("- não digas que consegues criar, apagar ou exportar recursos diretamente;");
            builder.AppendLine("- quando o utilizador perguntar como fazer algo, explica os passos dentro da interface da aplicação;");
            builder.AppendLine("- usa apenas os dados fornecidos no contexto atual da aplicação;");
            builder.AppendLine("- se não houver dados suficientes para responder, diz isso claramente.");
            builder.AppendLine();
            builder.AppendLine("Funcionalidades principais da aplicação:");
            builder.AppendLine("- Dashboard: mostra um resumo geral do cluster;");
            builder.AppendLine("- Nodes: mostra os nós do cluster e os respetivos detalhes;");
            builder.AppendLine("- Namespaces: permite consultar, criar, apagar e exportar namespaces;");
            builder.AppendLine("- Pods: permite consultar, criar, apagar e exportar pods;");
            builder.AppendLine("- Deployments: permite consultar, criar, apagar e exportar deployments;");
            builder.AppendLine("- Services: permite consultar, criar, apagar e exportar services;");
            builder.AppendLine("- Ingresses: permite consultar, criar, apagar e exportar ingresses;");
            builder.AppendLine("- ChatBot: permite fazer perguntas sobre a aplicação e sobre os recursos Kubernetes apresentados.");
            builder.AppendLine();
            builder.AppendLine("Instruções sobre exportação:");
            builder.AppendLine("- para exportar um recurso, o utilizador deve selecionar o recurso na respetiva tabela e clicar no botão Exportar;");
            builder.AppendLine("- depois deve escolher YAML ou JSON na mini janela apresentada;");
            builder.AppendLine("- por fim deve escolher a localização do disco onde pretende guardar o ficheiro;");
            builder.AppendLine("- por omissão, o nome do ficheiro usa o nome do recurso selecionado.");
            builder.AppendLine();
            builder.AppendLine("Instruções sobre criação e eliminação:");
            builder.AppendLine("- para criar um recurso, o utilizador deve abrir a aba respetiva e clicar no botão Criar;");
            builder.AppendLine("- para apagar um recurso, o utilizador deve selecionar o recurso na tabela e clicar no botão Eliminar;");
            builder.AppendLine("- se a operação falhar, o utilizador deve verificar permissões RBAC, ligação ao cluster, namespace selecionado e validade dos dados introduzidos.");
            builder.AppendLine();
            builder.AppendLine("Contexto atual da aplicação:");
            builder.AppendLine(BuildChatbotClusterContext());
            builder.AppendLine();
            builder.AppendLine("Pergunta do utilizador:");
            builder.AppendLine(question);

            return builder.ToString();
        }

        private string BuildChatbotClusterContext()
        {
            StringBuilder context = new StringBuilder();

            context.AppendLine("Ligação K3s: " + (string.IsNullOrWhiteSpace(baseUrl) ? "não definida" : baseUrl));
            context.AppendLine("Nodes carregados: " + SafeCount(nodeDetails));
            context.AppendLine("Namespaces carregados: " + SafeCount(namespaceDetails));
            context.AppendLine("Pods carregados: " + SafeCount(podDetails));
            context.AppendLine("Deployments carregados: " + SafeCount(deploymentDetails));
            context.AppendLine("Services carregados: " + SafeCount(serviceDetails));
            context.AppendLine("Ingresses carregados: " + SafeCount(ingressDetails));

            AppendNodeContext(context);
            AppendNamespaceContext(context);
            AppendPodContext(context);
            AppendDeploymentContext(context);
            AppendServiceContext(context);
            AppendIngressContext(context);

            return context.ToString();
        }

        private void AppendNodeContext(StringBuilder context)
        {
            if (nodeDetails == null || nodeDetails.Count == 0)
                return;

            context.AppendLine();
            context.AppendLine("Nodes:");

            foreach (KubernetesNodeDetails node in nodeDetails.Take(10))
                context.AppendLine("- " + node.Name + " | Estado: " + node.Status + " | IP: " + node.InternalIp + " | CPU: " + node.CpuCapacity + " | Memória: " + node.MemoryCapacityGiB + " GiB");
        }

        private void AppendNamespaceContext(StringBuilder context)
        {
            if (namespaceDetails == null || namespaceDetails.Count == 0)
                return;

            context.AppendLine();
            context.AppendLine("Namespaces:");

            foreach (KubernetesNamespaceDetails ns in namespaceDetails.Take(20))
                context.AppendLine("- " + ns.Name + " | Estado: " + ns.Phase + " | Criado em: " + ns.CreationTimestamp);
        }

        private void AppendPodContext(StringBuilder context)
        {
            if (podDetails == null || podDetails.Count == 0)
                return;

            context.AppendLine();
            context.AppendLine("Pods:");

            foreach (KubernetesPodDetails pod in podDetails.Take(30))
                context.AppendLine("- " + pod.Namespace + "/" + pod.Name + " | Fase: " + pod.Phase + " | Ready: " + pod.Ready + " | Restarts: " + pod.Restarts + " | Node: " + pod.NodeName + " | Imagens: " + ShortContextValue(pod.ImagesText, 120));
        }

        private void AppendDeploymentContext(StringBuilder context)
        {
            if (deploymentDetails == null || deploymentDetails.Count == 0)
                return;

            context.AppendLine();
            context.AppendLine("Deployments:");

            foreach (KubernetesDeploymentDetails deployment in deploymentDetails.Take(30))
                context.AppendLine("- " + deployment.Namespace + "/" + deployment.Name + " | Estado: " + deployment.Status + " | Ready: " + deployment.ReadyText + " | Réplicas: " + deployment.Replicas + " | Imagens: " + ShortContextValue(deployment.ImagesText, 120));
        }

        private void AppendServiceContext(StringBuilder context)
        {
            if (serviceDetails == null || serviceDetails.Count == 0)
                return;

            context.AppendLine();
            context.AppendLine("Services:");

            foreach (KubernetesServiceDetails service in serviceDetails.Take(30))
                context.AppendLine("- " + service.Namespace + "/" + service.Name + " | Tipo: " + service.Type + " | ClusterIP: " + service.ClusterIP + " | Portas: " + ShortContextValue(service.PortsText, 100) + " | Selector: " + ShortContextValue(service.SelectorText, 100));
        }

        private void AppendIngressContext(StringBuilder context)
        {
            if (ingressDetails == null || ingressDetails.Count == 0)
                return;

            context.AppendLine();
            context.AppendLine("Ingresses:");

            foreach (KubernetesIngressDetails ingress in ingressDetails.Take(30))
                context.AppendLine("- " + ingress.Namespace + "/" + ingress.Name + " | Hosts: " + ShortContextValue(ingress.HostsText, 100) + " | Services: " + ShortContextValue(ingress.ServicesText, 100) + " | Address: " + ingress.Address);
        }

        private int SafeCount<T>(List<T> list)
        {
            return list == null ? 0 : list.Count;
        }

        private string ShortContextValue(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "-";

            string clean = value.Replace("\r", " ").Replace("\n", " ").Trim();

            if (clean.Length <= maxLength)
                return clean;

            return clean.Substring(0, maxLength - 3) + "...";
        }

        private void AppendChatbotMessage(string author, string message)
        {
            if (rtbChatbotConversation == null)
                return;

            rtbChatbotConversation.AppendText("\n" + author + ":\n" + message.Trim() + "\n");
            rtbChatbotConversation.SelectionStart = rtbChatbotConversation.TextLength;
            rtbChatbotConversation.ScrollToCaret();
        }

        private void AppendChatbotSystemMessage(string message)
        {
            if (rtbChatbotConversation == null)
                return;

            rtbChatbotConversation.AppendText("\nSistema: " + message.Trim() + "\n");
            rtbChatbotConversation.SelectionStart = rtbChatbotConversation.TextLength;
            rtbChatbotConversation.ScrollToCaret();
        }
    }
}
