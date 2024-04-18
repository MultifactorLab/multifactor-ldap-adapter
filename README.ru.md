[![Лицензия](https://img.shields.io/badge/license-view-orange)](LICENSE.ru.md)

# multifactor-ldap-adapter

_Also available in other languages: [English](README.md)_

**MultiFactor Ldap Adapter** &mdash; программный компонент, LDAP proxy сервер для Linux. Используется для двухфакторной аутентификации пользователей в приложениях с LDAP аутентификацией.

Компонент является частью гибридного 2FA решения сервиса <a href="https://multifactor.ru/" target="_blank">MultiFactor</a>. Компонент доступен вместе с исходным кодом, распространяется бесплатно.

* <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter" target="_blank">Исходный код</a>
* <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter/releases" target="_blank">Сборка</a>

Windows-версия компонента доступна в репозитории <a href="https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter" target="_blank">MultiFactor.Ldap.Adapter</a>.

Дополнительные инструкции по интеграции 2FA в инфраструктуру доступны в <a href="https://multifactor.ru/docs/ldap-adapter/linux/" target="_blank">документации</a>.

## Содержание

- [Общие сведения](#общие-сведения)
  - [Функции компонента](#функции-компонента)
  - [Сценарии использования](#сценарии-использования)
- [Требования для установки компонента](#требования-для-установки-компонента)
- [Установка](#установка)
  - [Установка зависимостей](#установка-зависимостей)
    - [CentOS 7](#centos-7)
    - [CentOS 8](#centos-8)
    - [Ubuntu 18.04](#ubuntu-1804)
    - [Debian 10](#debian-10)
    - [Astra Linux Орёл](#astra-linux-орёл)
    - [Astra Linux Смоленск](#astra-linux-смоленск)
  - [Установка компонента](#установка-компонента)
- [Конфигурация](#конфигурация)
  - [Общие параметры](#общие-параметры)
- [Запуск компонента](#запуск-компонента)
- [Журналы](#журналы)
- [Сертификат для TLS шифрования](#сертификат-для-tls-шифрования)
- [Удаление компонента](#удаление-компонента)
  - [Удаление .NET Core](#удаление-net-core)
    - [CentOS 7](#centos-7-1)
    - [CentOS 8](#centos-8-1)
    - [Ubuntu 18.04](#ubuntu-1804-1)
    - [Debian 10](#debian-10-1)
    - [Astra Linux Орёл](#astra-linux-орёл-1)
    - [Astra Linux Смоленск](#astra-linux-смоленск-1)
  - [Удаление адаптера](#удаление-адаптера)
- [Лицензия](#лицензия)

## Общие сведения

### Функции компонента

Ключевые функции:

- проксирование сетевого трафика по протоколу LDAP;
- поиск запросов на аутентификацию и подтверждение вторым фактором на телефоне пользователя.

Основные возможности:

- работа по протоколам LDAP и LDAPS (шифрованный TLS канал);
- перехват запросов на аутентификацию, использующих механизмы Simple, Digital, NTLM;
- пропуск запросов от сервисных учетных записей (Bind DN) без второго фактора;
- запись журналов в Syslog сервер или SIEM систему.

### Сценарии использования

С помощью компонента можно реализовать следующие сценарии:

* Добавить второй фактор аутентификации в приложения, подключенные к Active Directory или другим LDAP каталогам;
* Включить шифрование трафика для приложений, которые не поддерживают подключение по TLS протоколу.

## Требования для установки компонента

- Компонент устанавливается на Linux сервер, протестирован на CentOS, Ubuntu, Debian, Astra Linux;
- Минимальные требования для сервера: 1 CPU, 2 GB RAM, 8 GB HDD (обеспечивают работу ОС и адаптера для 100 одновременных подключений &mdash; примерно 1500 пользователей);
- На сервере должны быть открыты TCP порты 389 (LDAP) и 636 (LDAPS) для приема запросов от клиентов;
- Серверу с установленным компонентом необходим доступ к хосту api.multifactor.ru по TCP порту 443 (TLS) напрямую или через HTTP proxy;
- Для взаимодействия с Active Directory, компоненту нужен доступ к серверу домена по TCP порту 389 (LDAP) или 636 (LDAPS);

## Установка

### Установка зависимостей

Компонент использует среду выполнения .NET 6 runtime, которая является бесплатной, открытой, разрабатывается компанией Microsoft и Open-Source сообществом. Среда выполнения не накладывает никаких ограничений на использование.

Для установки выполните команды:

#### CentOS 7

```shell
$ sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
$ sudo yum install aspnetcore-runtime-6.0
```
<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos</a>

#### CentOS 8

> ⚠️ **Warning**  
> CentOS Linux 8 достигла раннего окончания жизни (EOL) 31 декабря 2021 года.  
> Дополнительные сведения см. на официальной <a href="https://www.centos.org/centos-linux-eol/" target="_blank">странице</a> EOL Для CentOS Linux.
> Из-за этого .NET не поддерживается в CentOS Linux 8.

Дополнительную информацию см. на <a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos" target="_blank">странице</a>.  
См. также: <a href="https://learn.microsoft.com/ru-ru/dotnet/core/install/linux-rhel#supported-distributions">установка .NET на CentOS Stream</a>.

#### Ubuntu 18.04

```shell
$ wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb

$ sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-6.0
```
<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-ubuntu" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-ubuntu</a>

#### Debian 10

```shell
$ wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb

$ sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-6.0
```
<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-debian" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-debian</a>

#### Astra Linux Орёл

```shell
$ sudo apt install ca-certificates

$ wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
$ sudo mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
$ wget -q https://packages.microsoft.com/config/debian/9/prod.list
$ sudo mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
$ sudo chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
$ sudo chown root:root /etc/apt/sources.list.d/microsoft-prod.list

$ echo deb https://download.astralinux.ru/astra/current/orel/repository/ orel non-free main contrib | sudo tee -a /etc/apt/sources.list

$ sudo apt-get update
$ sudo apt-get install dotnet-sdk-6.0
```
Инструкция применима к Astra Linux Common Edition (релиз Орёл) и Special Edition (релиз Смоленск) с выключенным режимом замкнутой программной среды (ЗПС).
<a href="https://wiki.astralinux.ru/pages/viewpage.action?pageId=41192241#id-Смоленск1.6:УстановкаMS.NetCoreиMSVisualStudioCode-Загрузкаиустановкапакетов.NetCore" target="_blank">https://wiki.astralinux.ru/pages/viewpage.action?pageId=41192241#id-Смоленск1.6:УстановкаMS.NetCoreиMSVisualStudioCode-Загрузкаиустановкапакетов.NetCore</a>

#### Astra Linux Смоленск

```shell
$ sudo apt install ca-certificates
$ wget https://multifactor.ru/repo/dotnet+aspnetcore_amd64_signed.tar.gz && \
  wget https://multifactor.ru/repo/multifactor_pub.key
$ sudo cp multifactor_pub.key /etc/digisg/keys/ && \
  sudo cp multifactor_pub.key /etc/digsig/xattr_keys/ && \
  sudo rm multifactor_pub.key
$ sudo update-initramfs -u -k all
$ tar -xf dotnet+aspnetcore_amd64_signed.tar.gz
$ cd dotnet+aspnetcore_amd64_signed
$ sudo dpkg -i *.deb
```
Инструкция применима к Astra Linux Special Edition (релиз Смоленск) в режиме замкнутой программной среды (ЗПС).

### Установка компонента

Создайте папку, скачайте и распакуйте актуальную версию компонента из <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter/releases/" target="_blank">GitHub</a>:

```shell
$ sudo mkdir /opt/multifactor /opt/multifactor/ldap /opt/multifactor/ldap/tls /opt/multifactor/ldap/logs
$ sudo wget https://github.com/MultifactorLab/multifactor-ldap-adapter/releases/latest/download/release_linux_x64.zip
$ sudo unzip release_linux_x64.zip -d /opt/multifactor/ldap
```

Создайте системного пользователя mfa и дайте ему права на приложение:
```shell
$ sudo useradd -r mfa
$ sudo chown -R mfa: /opt/multifactor/ldap/
$ sudo chmod -R 700 /opt/multifactor/ldap/
```
Создайте службу
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

Включите автозапуск:
```shell
$ sudo systemctl enable multifactor-ldap
```

## Конфигурация

Параметры работы компонента хранятся в файле ```/opt/multifactor/ldap/multifactor-ldap-adapter.dll.config``` в формате XML.

### Общие параметры

```xml
<!-- Адрес и порт (TCP) по которому адаптер будет принимать запросы по протоколу LDAP -->
<!-- Если указать адрес 0.0.0.0, то адаптер будет слушать все сетевые интерфейсы-->
<add key="adapter-ldap-endpoint" value="0.0.0.0:389"/>

<!-- Адрес и порт (TCP) по которому адаптер будет принимать запросы по зашифрованному протоколу LDAPS -->
<!-- Если указать адрес 0.0.0.0, то адаптер будет слушать все сетевые интерфейсы-->
<add key="adapter-ldaps-endpoint" value="0.0.0.0:636"/>

<!-- Адрес или название домена Active Directory, а также схема подключения ldap или ldaps -->
<add key="ldap-server" value="ldaps://domain.local"/>

<!-- 
    Base DN для биндинга пользователя. 
    Пример: cn=users,cn=accounts,dc=domain,dc=local 
-->
<!--<add key="ldap-base-dn" value=""/>-->

<!-- Список сервисных учетных записей, которым не требуется второй фактор, перечисленные через точку с запятой -->
<add key="ldap-service-accounts" value="CN=Service Acc,OU=Users,DC=domain,DC=local"/>


<!--Адрес API Мультифактора -->
<add key="multifactor-api-url" value="https://api.multifactor.ru"/>
<!--Таймаут запросов в API Мультифактора, минимальное значение 65 секунд -->
<add key="multifactor-api-timeout" value="00:01:05"/>
<!-- Параметр NAS-Identifier для подключения к API Мультифактора - из личного кабинета -->
<add key="multifactor-nas-identifier" value=""/>
<!-- Параметр Shared Secret для подключения к API Мультифактора - из личного кабинета -->
<add key="multifactor-shared-secret" value=""/>

<!--Доступ к API Мультифактора через HTTP прокси (опционально)-->
<!--add key="multifactor-api-proxy" value="http://proxy:3128"/-->

<!-- Уровень логирования: 'Debug', 'Info', 'Warn', 'Error' -->
<add key="logging-level" value="Debug"/>
<!--certificate password leave empty or null for certificate without password-->
<!--<add key="certificate-password" value="XXXXXX"/>-->
```

## Запуск компонента

После настройки конфигурации запустите компонент:
```shell
$ sudo systemctl start multifactor-ldap
```
Статус можно проверить командой:

```shell
$ sudo systemctl status multifactor-ldap
```

## Журналы

Журналы работы компонента находятся в папке ```/opt/multifactor/ldap/logs```, а также в системном журнале.

## Сертификат для TLS шифрования

Если включена схема LDAPS, адаптер при первом запуске создаст самоподписанный SSL сертификат и сохранит его в папке /tls в формате pfx без пароля.
Этот сертификат будет использоваться для аутентификации сервера и шифрования трафика. Вы можете заменить его на ваш сертификат при необходимости.

## Удаление компонента

### Удаление .NET Core

Для просмотра списка установленных на вашей машине версий SDK и сред выполнения .NET Core используйте команду:

```shell
dotnet --info
```

Далее, выполните команды:

#### CentOS 7

```shell
$ sudo yum remove aspnetcore-runtime-6.0
```

#### CentOS 8

```shell
$ sudo dnf remove aspnetcore-runtime-6.0
```

#### Ubuntu 18.04

```shell
$ sudo apt-get remove aspnetcore-runtime-6.0
```

#### Debian 10

```shell
$ sudo apt-get remove aspnetcore-runtime-6.0
```

#### Astra Linux Орёл

```shell
$ sudo apt-get remove dotnet-sdk-6.0
```

#### Astra Linux Смоленск

```shell
$ sudo apt purge dotnet-* aspnetcore-*
```

### Удаление адаптера

Остановите службу ```multifactor-ldap```, удалите её из автозапуска и удалите конфигурационный файл юнита:

```shell
$ sudo systemctl stop multifactor-ldap
$ sudo systemctl disable multifactor-ldap
$ sudo rm /etc/systemd/system/multifactor-ldap.service
```

Перезагрузите настройки systemd, просканировав систему на наличие изменённых юнитов:

```shell
$ sudo systemctl daemon-reload
```

Удалите файлы адаптера и системного пользователя ```mfa```:

```shell
$ sudo rm -rf /opt/multifactor/ldap/
$ sudo userdel -r mfa
```

## Лицензия

Обратите внимание на [лицензию](LICENSE.ru.md). Она не дает вам право вносить изменения в исходный код Компонента и создавать производные продукты на его основе. Исходный код предоставляется в ознакомительных целях.
