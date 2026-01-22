

[![License](https://img.shields.io/badge/license-view-orange)](LICENSE.md)

# Multifactor LDAP adapter

_Also available in other languages: [Русский](README.ru.md)_

## What is MultiFactor Ldap Adapter?

**MultiFactor Ldap Adapter** is a LDAP proxy server for Linux. It allows you to quickly add multifactor authentication to your applications with LDAP authentication.

The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution. It is available with the source code and distributed for free.
* <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter" target="_blank">Source code</a>
* <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter/releases" target="_blank">Releases</a>

## Table of Contents

- [What is MultiFactor Ldap Adapter](#what-is-multiFactor-ldap-adapter)
- [Component Features](#component-features)
- [Use Cases](#use-cases)
- [Installation and configuration](#installation-and-configuration)
- [License](#license)

## Component Features

Key functionality:

- Proxying network traffic through LDAP protocol;
- Searching for authentication requests and confirming access on the user's phone with the second factor.

Key features:

- LDAP and LDAPS (encrypted TLS channel) support;
- Interception of authentication requests that use Simple, Digital, NTLM mechanisms;
- Bypassing requests from service accounts (Bind DN) without the second factor;
- Logging to Syslog server or SIEM system.

## Use Cases

Use LDAP Adapter Component to implement the following scenarios:

* Add a second authentication factor to applications connected to Active Directory or other LDAP directories;
* Enable traffic encryption for applications that do not support encrypted TLS connection.

## Installation and configuration
See [knowledge base](https://multifactor.pro/docs/ldap-adapter/linux/) for information about configuration, launch and an additional guidance.

## License

Please note, the [license](LICENSE.md) does not entitle you to modify the source code of the Component or create derivative products based on it. The source code is provided as-is for evaluation purposes.