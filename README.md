<p align="center">
  <img src="assets/icon-256.png" width="96" alt="SleepyWindows icon">
</p>

<h1 align="center">SleepyWindows</h1>

<p align="center">
  Um timer simples e portátil para desligar, reiniciar, suspender ou hibernar o Windows.<br>
  A simple, portable timer to shut down, restart, sleep or hibernate Windows.
</p>

<p align="center">
  <a href="https://github.com/MaloneFreak/SleepyWindows/releases/latest"><b>⬇ Download SleepyWindows.exe</b></a>
</p>

<p align="center">
  <img src="docs/preview.png" alt="SleepyWindows: tela inicial e contagem regressiva" width="700">
</p>

<p align="center"><a href="#português">Português</a> · <a href="#english">English</a></p>

---

## Português

### O que faz

- Escolha a ação: **Desligar**, **Reiniciar**, **Suspender** ou **Hibernar**.
- Clique num tempo pronto (10 min, 20 min, 30 min, 45 min, 1 h, 1h30, 2 h, 3 h) e pronto, já está agendado.
- Ou digite um tempo personalizado em **segundos, minutos ou horas** (até 999 horas) e clique em **Agendar** (ou aperte Enter).
- Mostra a contagem regressiva e o horário exato em que vai acontecer.
- **+15 min** adia, **Cancelar** desiste.
- **Portátil**: um único `.exe` com menos de 200 KB. Não instala nada e não grava nada no registro.
- Interface em português ou inglês, de acordo com o idioma do Windows.

### Como usar

1. Baixe o `SleepyWindows.exe` em [Releases](https://github.com/MaloneFreak/SleepyWindows/releases/latest) e coloque onde quiser (área de trabalho, pendrive...).
2. Abra. Se aparecer "O Windows protegeu o computador", clique em **Mais informações → Executar assim mesmo**. Isso acontece com qualquer programa sem assinatura digital paga.
3. Escolha a ação e o tempo.

### Como funciona

- **Desligar e Reiniciar** usam o agendador do próprio Windows (`shutdown /s /t` e `shutdown /r /t`).
  Depois de agendar, **pode fechar o app**: o Windows desliga do mesmo jeito.
  Para desistir depois de ter fechado, abra o app de novo e clique em *Cancelar um desligamento já agendado* (é o mesmo que `shutdown /a`).
  Atenção: com um tempo maior que zero, o Windows fecha os programas abertos à força, então salve seus arquivos.
- **Suspender e Hibernar** não têm timer no Windows, então o próprio app conta o tempo e suspende no final.
  Nesse modo o **app precisa ficar aberto** (pode minimizar). Um minuto antes, a janela volta para a frente para dar tempo de cancelar.
- **Hibernar** aparece apagado se a hibernação estiver desativada no PC. Para ativar, rode `powercfg /h on` num Prompt de Comando como administrador.

### Requisitos

Windows 10 ou 11. Não precisa instalar nada: o app usa o .NET Framework que já vem com o Windows.
Deve funcionar também no Windows 7 e 8.1 com o .NET Framework 4.6.2 ou mais novo instalado.

### Compilar a partir do código

Com o [.NET SDK](https://dotnet.microsoft.com/download) 6 ou mais novo:

```
dotnet build -c Release
```

O executável sai em `bin/Release/net462/SleepyWindows.exe`.
Cada tag `v*` enviada ao GitHub compila o app no Windows e publica uma release automaticamente (veja [`.github/workflows/build.yml`](.github/workflows/build.yml)).

---

## English

### What it does

- Pick an action: **Shut down**, **Restart**, **Sleep** or **Hibernate**.
- Click a preset (10 min, 20 min, 30 min, 45 min, 1 h, 1h30, 2 h, 3 h) and it's scheduled.
- Or type a custom time in **seconds, minutes or hours** (up to 999 hours) and click **Schedule** (or press Enter).
- Shows a countdown and the exact time it will happen.
- **+15 min** to snooze, **Cancel** to call it off.
- **Portable**: a single `.exe` under 200 KB. Nothing to install, nothing written to the registry.
- English or Portuguese interface, following your Windows language.

### How to use

1. Download `SleepyWindows.exe` from [Releases](https://github.com/MaloneFreak/SleepyWindows/releases/latest) and put it anywhere (desktop, USB stick...).
2. Run it. If you see "Windows protected your PC", click **More info → Run anyway**. That happens with any app that doesn't have a paid code-signing certificate.
3. Pick the action and the time.

### How it works

- **Shut down and Restart** use Windows' own scheduler (`shutdown /s /t` and `shutdown /r /t`).
  Once scheduled, **you can close the app**: Windows will still shut down.
  To call it off after closing the app, open it again and click *Cancel a shutdown that's already scheduled* (same as `shutdown /a`).
  Note: with a timeout above zero, Windows force-closes open programs, so save your work.
- **Sleep and Hibernate** have no built-in timer in Windows, so the app keeps the countdown itself and suspends the PC at the end.
  In this mode the **app has to stay open** (minimizing is fine). One minute before, the window comes back to the front so you can still cancel.
- **Hibernate** is greyed out when hibernation is turned off. To turn it on, run `powercfg /h on` in an administrator Command Prompt.

### Requirements

Windows 10 or 11. Nothing to install: the app runs on the .NET Framework that ships with Windows.
It should also work on Windows 7 and 8.1 with .NET Framework 4.6.2 or newer installed.

### Building from source

With the [.NET SDK](https://dotnet.microsoft.com/download) 6 or newer:

```
dotnet build -c Release
```

The executable ends up in `bin/Release/net462/SleepyWindows.exe`.
Every `v*` tag pushed to GitHub builds the app on Windows and publishes a release automatically (see [`.github/workflows/build.yml`](.github/workflows/build.yml)).

---

## Project layout

```
src/
  Program.cs        entry point, single-instance check
  MainForm.cs       the window: presets, custom time, countdown
  PowerControl.cs   shutdown.exe calls and sleep/hibernate
  Controls.cs       dark theme, rounded buttons, number box
  Strings.cs        Portuguese / English text
  NativeMethods.cs  dark title bar, hibernate check
assets/             icons
```

## License

[MIT](LICENSE)
