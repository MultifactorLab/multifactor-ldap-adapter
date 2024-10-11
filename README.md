[![License](https://img.shields.io/badge/license-view-orange)](LICENSE.md)

# multifactor-ldap-adapter

_Also available in other languages: [Русский](README.ru.md)_

**MultiFactor Ldap Adapter** is a LDAP proxy server for Linux. It allows you to quickly add multifactor authentication to your applications with LDAP authentication.

The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution. It is available with the source code and distributed for free.

* <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter" target="_blank">Source code</a>
* <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter/releases" target="_blank">Build</a>

Windows version of the component is available in <a href="https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter" target="_blank">MultiFactor.Ldap.Adapter</a> repository.

See <a href="https://multifactor.pro/docs/ldap-adapter/linux/" target="_blank">knowledge base</a> for additional guidance on integrating 2FA through LDAP into your infrastructure.

## Table of Contents

- [Overview](#overview)
  - [Component Features](#component-features)
  - [Use Cases](#use-cases)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
  - [Dependencies Installation](#dependencies-installation)
    - [CentOS 7](#centos-7)
    - [CentOS 8](#centos-8)
    - [Ubuntu 18.04](#ubuntu-1804)
    - [Debian 10](#debian-10)
  - [Component Installation](#component-installation)
- [Configuration](#configuration)
  - [General Parameters](#general-parameters)
- [Start-Up](#start-up)
- [Logs](#logs)
- [Certificate for TLS encryption](#certificate-for-tls-encryption)
- [Uninstallation](#uninstallation)
  - [Uninstall .NET Core](#uninstall-net-core)
    - [CentOS 7](#centos-7-1)
    - [CentOS 8](#centos-8-1)
    - [Ubuntu 18.04](#ubuntu-1804-1)
    - [Debian 10](#debian-10-1)
  - [Uninstall Component](#uninstall-component)
- [License](#license)

## Overview

### Component Features

Key functionality:

- Proxying network traffic through LDAP protocol;
- Searching for authentication requests and confirming access on the user's phone with the second factor.

Key features:

- LDAP and LDAPS (encrypted TLS channel) support;
- Interception of authentication requests that use Simple, Digital, NTLM mechanisms;
- Bypassing requests from service accounts (Bind DN) without the second factor;
- Logging to Syslog server or SIEM system.

### Use Cases

Use LDAP Adapter Component to implement the following scenarios:

* Add a second authentication factor to applications connected to Active Directory or other LDAP directories;
* Enable traffic encryption for applications that do not support encrypted TLS connection.

## Prerequisites

- Component is installed on a Linux server, tested on CentOS, Ubuntu, Debian;
- Minimum server requirements: 1 CPU, 2 GB RAM, 8 GB HDD (to run the OS and adapter for 100 simultaneous connections &mdash; approximately 1500 users);
- TCP ports 389 (LDAP) and 636 (LDAPS) must be open on the server to receive requests from clients;
- The server with the component installed needs access to ```api.multifactor.ru``` via TCP port 443 (TLS) directly or via HTTP proxy;
- To interact with Active Directory, the component needs access to the domain server via TCP port 389 (LDAP) or 636 (LDAPS);

## Installation

### Dependencies Installation

The component uses the .NET 8 runtime environment, which is free, open-source, developed by Microsoft and the open-source community. The runtime environment does not impose any restrictions on its use.

To install, run the commands:

#### CentOS 7

```shell
$ sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
$ sudo yum install aspnetcore-runtime-8.0
```
<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos</a>


#### CentOS 8

> ⚠️ **Warning**  
> CentOS Linux 8 reached an early End Of Life (EOL) on December 31st, 2021.  
> For more information, see the official <a href="https://www.centos.org/centos-linux-eol/" target="_blank">CentOS Linux EOL page</a>.
> Because of this, .NET isn't supported on CentOS Linux 8.

For more information see <a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos" target="_blank">this page</a>.  
See also: <a href="https://learn.microsoft.com/ru-ru/dotnet/core/install/linux-rhel#supported-distributions">install the .NET on CentOS Stream</a>.

#### Ubuntu 18.04

```shell
$ wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb

$ sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-8.0
```

<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-ubuntu" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-ubuntu</a>

#### Debian 10

```shell
$ wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb

$ sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-8.0
```

<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-debian" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-debian</a>

### Component Installation

Create a folder, download and unzip the current version of the component from <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter/releases/" target="_blank">GitHub</a>:

```shell
$ sudo mkdir /opt/multifactor /opt/multifactor/ldap /opt/multifactor/ldap/tls /opt/multifactor/ldap/logs
$ sudo wget https://github.com/MultifactorLab/multifactor-ldap-adapter/releases/latest/download/release_linux_x64.zip
$ sudo unzip release_linux_x64.zip -d /opt/multifactor/ldap
```

Create a system user mfa and give it rights to the application:
```shell
$ sudo useradd -r mfa
$ sudo chown -R mfa: /opt/multifactor/ldap/
$ sudo chmod -R 700 /opt/multifactor/ldap/
```
Create a service
```shell
$ sudo vi /etc/systemd/system/multifactor-ldap.service
```

```shell
[Unit]
Description=Multifactor Ldap Adapter

[Service]
WorkingDirectory=/opt/multifactor/ldap/
ExecStart=/usr/bin/dotnet /opt/multifactor/ldap/multifactor-ldap-adapter.dll
Restart=always
# Restart service after 10 seconds if the service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=multifactor-ldap
User=mfa
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
 
# How many seconds to wait for the app to shut down after it receives the initial interrupt signal. 
# If the app doesn't shut down in this period, SIGKILL is issued to terminate the app. 
# The default timeout for most distributions is 90 seconds.
TimeoutStopSec=30

# give the executed process the CAP_NET_BIND_SERVICE capability. This capability allows the process to bind to well known ports.
AmbientCapabilities=CAP_NET_BIND_SERVICE

[Install]
WantedBy=multi-user.target
```

Enable autorun:
```shell
$ sudo systemctl enable multifactor-ldap
```
## Configuration

The component's parameters are stored in ```/opt/multifactor/ldap/multifactor-ldap-adapter.dll.config``` in XML format.

### General Parameters

```xml
<!-- The address and port (TCP) on which the adapter will listen to LDAP requests -->
<!-- If you specify 0.0.0.0, then the adapter will listen on all network interfaces -->
<add key="adapter-ldap-endpoint" value="0.0.0.0:389"/>

<!-- The address and port (TCP) on which the adapter will listen for LDAPS encrypted requests -->
<!-- If you specify 0.0.0.0, then the adapter will listen on all network interfaces -->
<add key="adapter-ldaps-endpoint" value="0.0.0.0:636"/>

<!-- Active Directory domain address or name, and ldap or ldaps connection scheme -->
<add key="ldap-server" value="ldaps://domain.local"/>

<!-- 
    DN for user binding. Example: cn=users,cn=accounts,dc=domain,dc=local 
-->
<!--<add key="ldap-base-dn" value=""/>-->

<!-- List of service accounts that do not require a second factor, separated by semicolons -->
<add key="ldap-service-accounts" value="CN=Service Acc,OU=Users,DC=domain,DC=local"/>

<!-- Multifactor API address -->
<add key="multifactor-api-url" value="https://api.multifactor.ru"/>
<!--Timeout for requests in the Multifactor API, the minimum value is 65 seconds-->
<add key="multifactor-api-timeout" value="00:01:05"/>
<!-- NAS-Identifier parameter to connect to the Multifactor API - from resource details in your account -->
<add key="multifactor-nas-identifier" value=""/>
<!-- Shared Secret parameter to connect to the Multifactor API - from resource details in your account -->
<add key="multifactor-shared-secret" value=""/>

<!-- Access to the Multifactor API via HTTP proxy (optional) -->
<!--add key="multifactor-api-proxy" value="http://proxy:3128"/-->

<!-- Logging level: 'Debug', 'Info', 'Warn', 'Error' -->
<add key="logging-level" value="Debug"/>
<!--certificate password leave empty or null for certificate without password-->
<!--<add key="certificate-password" value="XXXXXX"/>-->
```

## Start-Up

After configuring the configuration, run the component:
```shell
$ sudo systemctl start multifactor-ldap
```
You can check the status with the command:

```shell
$ sudo systemctl status multifactor-ldap
```

## Logs

The logs of the component are located in the ```/opt/multifactor/ldap/logs``` folder as well as in the system log.

## Certificate for TLS Encryption

If the LDAPS scheme is enabled, the adapter creates a self-signed SSL certificate the first time it starts up, and saves it in the /tls folder in pfx format without a password.
This certificate will be used for server authentication and traffic encryption. You can replace it with your own certificate if necessary.

## Uninstallation

### Uninstall .NET Core

To view a list of SDK versions and the .NET Core runtimes installed on your machine use the command:

```shell
dotnet --info
```

Next, run the commands:

#### CentOS 7

```shell
$ sudo yum remove aspnetcore-runtime-8.0
```

#### CentOS 8

```shell
$ sudo dnf remove aspnetcore-runtime-8.0
```

#### Ubuntu 18.04

```shell
$ sudo apt-get remove aspnetcore-runtime-8.0
```

#### Debian 10

```shell
$ sudo apt-get remove aspnetcore-runtime-8.0
```

### Uninstall Component

Stop the ``multifactor-ldap`` service, remove it from the autorun and delete the unit configuration file:

```shell
$ sudo systemctl stop multifactor-ldap
$ sudo systemctl disable multifactor-ldap
$ sudo rm /etc/systemd/system/multifactor-ldap.service
```

Reload the systemd settings by scanning the system for changed units:

```shell
$ sudo systemctl daemon-reload
```

Delete the adapter files and system user ```mfa```:

```shell
$ sudo rm -rf /opt/multifactor/ldap/
$ sudo userdel -r mfa
```

## License

Please note, the [license](LICENSE.md) does not entitle you to modify the source code of the Component or create derivative products based on it. The source code is provided as-is for evaluation purposes.
