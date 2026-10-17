
# ☸️ Kubernetes Controller

### Desktop Application for Kubernetes & K3s Cluster Management

![C#](https://img.shields.io/badge/C%23-.NET%20WinForms-512BD4?logo=dotnet&logoColor=white)
![Kubernetes](https://img.shields.io/badge/Kubernetes-Cluster%20Management-326CE5?logo=kubernetes&logoColor=white)
![K3s](https://img.shields.io/badge/K3s-Lightweight%20Kubernetes-FFC61C?logo=k3s&logoColor=black)
![REST API](https://img.shields.io/badge/REST%20API-Kubernetes%20API-009688)
![Ollama](https://img.shields.io/badge/Ollama-Local%20AI-222222)
![License](https://img.shields.io/badge/License-MIT-green)

---

## 📖 Overview

**Kubernetes Controller** is a desktop application developed in **C# using Windows Forms (.NET)** to simplify the management and administration of **Kubernetes clusters running on K3s**.

The application provides a graphical interface for interacting with Kubernetes resources through the **Kubernetes REST API**, allowing users to inspect cluster information, manage workloads, and export resource configurations without relying exclusively on command-line tools.

It also integrates a **local AI assistant powered by Ollama (Llama 3.2 1B)**, designed to provide contextual explanations about Kubernetes concepts and application functionality.

The project was developed in an academic environment to explore:

- Kubernetes cluster administration and orchestration
- REST API integration and HTTP communication
- Role-Based Access Control (RBAC)
- Desktop application development with C# and .NET
- Local AI integration using Ollama

> **Project scope:** This application is intended for educational purposes and controlled laboratory environments. It is not designed as a production-ready Kubernetes management platform.

---

## 🖥️ Application Preview

![Kubernetes Controller Dashboard](docs/images/dashboard.png)

*Main dashboard providing an overview of the connected Kubernetes cluster and its resources.*

---

## 🎯 Project Objectives

The main objective of this project is to develop a centralized desktop interface that simplifies common Kubernetes administration tasks while providing practical experience in cluster management and software integration.

The project focuses on:

1. **Centralized Management** — Provide a graphical interface to interact with Kubernetes cluster resources.
2. **Resource Administration** — Perform supported operations on workloads and infrastructure resources.
3. **Cluster Visibility** — Display cluster information, resource states, and available capacity.
4. **Configuration Export** — Retrieve and export Kubernetes resource definitions in YAML and JSON formats.
5. **Secure API Communication** — Authenticate requests using Kubernetes ServiceAccounts and RBAC.
6. **AI-Assisted Learning** — Integrate a local language model to explain Kubernetes concepts and application features.

---

## ✨ Key Features

### 📊 Cluster Overview

- View connected Kubernetes cluster information.
- Inspect cluster nodes and their status.
- Display available CPU and memory capacity information.
- Visualize Kubernetes workload and resource states.
- Navigate between supported resource categories.

### ☸️ Kubernetes Resource Management

Manage supported Kubernetes resources through a graphical interface.

| Resource | Supported Operations |
|---|---|
| **Namespaces** | View, create and delete |
| **Pods** | View, inspect and delete |
| **Deployments** | View, create and delete |
| **Services** | View, create and delete |
| **Ingresses** | View, create and delete |

*Available operations depend on the functionality implemented in the application and the permissions granted to the configured Kubernetes ServiceAccount.*

### 📄 YAML & JSON Export

- Retrieve Kubernetes resource configurations through the REST API.
- Export supported resources to YAML or JSON files.
- Inspect exported definitions for learning, documentation, and configuration analysis.

### 🤖 Local AI Assistant — Ollama

The application integrates an AI assistant using **Ollama** and **Llama 3.2 1B**.

The assistant is designed to:

- Explain Kubernetes and K3s concepts.
- Provide guidance on supported application features.
- Help users understand common resource types and operations.
- Operate using a locally hosted language model.

**Important:** The AI assistant provides informational support only. It does not directly execute Kubernetes operations or modify cluster resources. Generated responses may require verification.

### 🔐 Kubernetes API Authentication

- Communicate with the Kubernetes API server over HTTPS.
- Authenticate requests using bearer tokens.
- Use Kubernetes ServiceAccounts for API access.
- Apply Role-Based Access Control (RBAC) to manage permissions.

> **Security note:** The example configuration included in this repository is intended for a controlled laboratory environment. Production environments should use least-privilege RBAC permissions, secure credential storage, and appropriate network access controls.

---

## 🏗️ System Architecture

The application follows a client-server communication model:

```text
┌─────────────────────────────────────────┐
│          Kubernetes Controller          │
│             C# / WinForms               │
│                                         │
│  Dashboard | Resources | AI Assistant   │
└───────────────────┬─────────────────────┘
                    │
          HTTPS / REST API
                    │
                    ▼
┌─────────────────────────────────────────┐
│              K3s Cluster                │
│            Ubuntu Server VM             │
│                                         │
│  Kubernetes API Server                  │
│  ├── Namespaces                         │
│  ├── Pods                               │
│  ├── Deployments                        │
│  ├── Services                           │
│  └── Ingresses                          │
└─────────────────────────────────────────┘

           Separate AI Integration

┌─────────────────────────────────────────┐
│          Kubernetes Controller          │
│              AI Assistant               │
└───────────────────┬─────────────────────┘
                    │
                  HTTP
                    │
                    ▼
┌─────────────────────────────────────────┐
│                Ollama                   │
│             Llama 3.2 1B                │
│          Locally Hosted Model           │
└─────────────────────────────────────────┘
```

**Architecture Summary:**

- **Desktop Client:** C# Windows Forms application providing the user interface.
- **Kubernetes API:** REST-based communication with the K3s cluster.
- **K3s Environment:** Lightweight Kubernetes cluster hosted on an Ubuntu Server virtual machine.
- **Authentication:** Bearer-token authentication using a Kubernetes ServiceAccount.
- **AI Integration:** Independent HTTP communication with a locally hosted Ollama instance.

---

## Technologies

- C#;
- Windows Forms;
- Kubernetes REST API;
- K3s;
- JSON;
- YAML;
- HTTP and HTTPS;
- bearer token authentication;
- Kubernetes RBAC;
- Ollama;
- Llama 3.2 1B;
- Ubuntu Server VM;
- Visual Studio.

---

## Requirements

### Windows computer

- Windows 10 or Windows 11;
- Visual Studio with C# and Windows desktop development support;
- network access to the Ubuntu VM.

### Ubuntu virtual machine

- Ubuntu Server;
- K3s installed and running;
- Ollama installed and running;
- `llama3.2:1b` installed;
- ports `6443` and `11434` accessible from the Windows host.

---

## K3s Access Configuration

The application requires a Kubernetes ServiceAccount and the necessary RBAC permissions.

Create a file named:

```text
kubernetes-controller-access.yaml
```

Add the following configuration:

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: kubernetes-controller
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: kubernetes-controller-sa
  namespace: kubernetes-controller
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: kubernetes-controller-admin
subjects:
  - kind: ServiceAccount
    name: kubernetes-controller-sa
    namespace: kubernetes-controller
roleRef:
  kind: ClusterRole
  name: cluster-admin
  apiGroup: rbac.authorization.k8s.io
```

Apply it:

```bash
sudo k3s kubectl apply -f kubernetes-controller-access.yaml
```

Generate a token:

```bash
sudo k3s kubectl create token kubernetes-controller-sa \
  -n kubernetes-controller \
  --duration=8760h
```

Copy the generated token and use it on the application connection screen.

> The `cluster-admin` role is convenient for a controlled educational environment, but it provides unrestricted access to the cluster. A production deployment should use a dedicated least-privilege ClusterRole.

---

## Ollama Configuration

Confirm that Ollama is installed:

```bash
ollama --version
```

Confirm that the model is available:

```bash
ollama list
```

Install the model when required:

```bash
ollama pull llama3.2:1b
```

Test it locally:

```bash
ollama run llama3.2:1b
```

By default, Ollama may only listen on `localhost`. To allow the Windows application to connect to it, edit the systemd service:

```bash
sudo systemctl edit ollama.service
```

Add:

```ini
[Service]
Environment="OLLAMA_HOST=0.0.0.0:11434"
```

Reload and restart the service:

```bash
sudo systemctl daemon-reload
sudo systemctl restart ollama
```

Check its state:

```bash
sudo systemctl status ollama --no-pager
```

Test it from the Windows computer:

```powershell
curl http://VM_IP:11434
```

Expected response:

```text
Ollama is running
```

---

## Running the Application

1. Start the Ubuntu VM.
2. Confirm that K3s is running:

   ```bash
   sudo systemctl status k3s --no-pager
   ```

3. Confirm that Ollama is running:

   ```bash
   sudo systemctl status ollama --no-pager
   ```

4. Open the solution in Visual Studio.
5. Build the project.
6. Start the application.
7. Enter the Ubuntu VM IP address.
8. Paste the Kubernetes ServiceAccount token.
9. Connect to the cluster.

The application normally builds the Kubernetes API URL as:

```text
https://VM_IP:6443
```

When no token is provided, the application may use a `kubectl proxy` connection such as:

```text
http://VM_IP:8001
```

---

## Kubernetes API Endpoints

The application uses endpoints such as:

```text
GET /version
GET /api/v1/nodes
GET /api/v1/namespaces
GET /api/v1/pods
GET /apis/apps/v1/deployments
GET /api/v1/services
GET /apis/networking.k8s.io/v1/ingresses
```

Namespaced operations use endpoints such as:

```text
/api/v1/namespaces/{namespace}/pods
/apis/apps/v1/namespaces/{namespace}/deployments
/api/v1/namespaces/{namespace}/services
/apis/networking.k8s.io/v1/namespaces/{namespace}/ingresses
```

Create and delete operations are performed through the corresponding Kubernetes REST API endpoints.

---


```text
KubernetesController/
│
├── Project Overview
├── Main Screenshot
├── Key Features
├── System Architecture
├── Technologies Used
├── Application Screenshots
├── Requirements
├── Getting Started
│   ├── K3s Access Configuration
│   ├── Ollama Configuration
│   └── Running the Application
├── Kubernetes API Endpoints
├── Repository Structure
├── Security Considerations
├── Known Limitations
├── Future Improvements
├── Author and Contributions
└── License
```

The exact structure may vary depending on the current project version.

---

## Security Notes

Do not commit the following information to GitHub:

- Kubernetes bearer tokens;
- ServiceAccount tokens;
- credentials;
- kubeconfig files containing certificates or tokens;
- exported resources containing secrets.

Recommended `.gitignore` entries:

```gitignore
.vs/
bin/
obj/
*.user
*.suo
*.cache
*.log
appsettings.local.json
secrets.json
token.txt
kubeconfig
```

If a token is accidentally exposed, generate a new one and stop using the previous token.

---

## Known Limitations

- The application is intended primarily for educational and controlled laboratory environments.
- The current chatbot uses a small 1B parameter model, so complex answers may be limited.
- Chatbot availability depends on the Ubuntu VM and the Ollama service.
- The chatbot is informational and does not execute cluster operations.
- XML export is not supported.
- The current RBAC example uses `cluster-admin`.
- Advanced resources such as ConfigMaps, Secrets, StatefulSets, DaemonSets, Jobs, CronJobs, and persistent volumes are not yet managed by the interface.

---

## Possible Future Improvements

- least-privilege Kubernetes RBAC;
- support for ConfigMaps and Secrets;
- support for StatefulSets, DaemonSets, Jobs, and CronJobs;
- pod log visualization;
- Kubernetes event visualization;
- resource editing;
- YAML import and deployment;
- filtering and searching;
- namespace-specific views;
- configurable Ollama model and endpoint;
- chatbot conversation history;
- more detailed cluster health diagnostics;
- secure configuration storage;
- automated tests;
- installer or release package.

---

## Educational Purpose

This project demonstrates practical knowledge in:

- Kubernetes resource management;
- K3s administration;
- Kubernetes REST API usage;
- authentication and RBAC;
- asynchronous HTTP communication in C#;
- JSON and YAML processing;
- WinForms interface development;
- local AI model integration;
- virtual machine networking;
- software documentation.

---

## License

This project is licensed under the MIT License.
See the [LICENSE](LICENSE) file for details.

---

## Author

Developed as an academic and portfolio project focused on Kubernetes, K3s, C# WinForms, REST APIs, and local artificial intelligence.
