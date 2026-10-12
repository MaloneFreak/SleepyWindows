using System;
using System.Globalization;

namespace SleepyWindows
{
    /// <summary>
    /// UI text. Portuguese when Windows is in Portuguese, English otherwise.
    /// To add a language, add a check in <see cref="Pick"/> and one more argument per string.
    /// </summary>
    internal static class Strings
    {
        private static readonly bool Portuguese =
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt";

        private static string Pick(string pt, string en)
        {
            return Portuguese ? pt : en;
        }

        public const string AppName = "SleepyWindows";

        public static string WhatToDo => Pick("O que fazer?", "What should happen?");
        public static string InHowLong => Pick("Em quanto tempo?", "In how long?");
        public static string CustomTime => Pick("Tempo personalizado", "Custom time");

        public static string Seconds => Pick("seg", "sec");
        public static string Minutes => "min";
        public static string Hours => "h";

        public static string Schedule => Pick("Agendar", "Schedule");
        public static string Snooze => "+15 min";
        public static string Cancel => Pick("Cancelar", "Cancel");
        public static string CancelExisting => Pick("Cancelar um desligamento já agendado", "Cancel a shutdown that's already scheduled");

        public static string ActionName(PowerAction action)
        {
            switch (action)
            {
                case PowerAction.Restart: return Pick("Reiniciar", "Restart");
                case PowerAction.Sleep: return Pick("Suspender", "Sleep");
                case PowerAction.Hibernate: return Pick("Hibernar", "Hibernate");
                default: return Pick("Desligar", "Shut down");
            }
        }

        public static string CountdownTitle(PowerAction action)
        {
            switch (action)
            {
                case PowerAction.Restart: return Pick("Reiniciando em", "Restarting in");
                case PowerAction.Sleep: return Pick("Suspendendo em", "Going to sleep in");
                case PowerAction.Hibernate: return Pick("Hibernando em", "Hibernating in");
                default: return Pick("Desligando em", "Shutting down in");
            }
        }

        public static string WindowsComment(PowerAction action)
        {
            return action == PowerAction.Restart
                ? Pick("Reinício agendado pelo SleepyWindows.", "Restart scheduled by SleepyWindows.")
                : Pick("Desligamento agendado pelo SleepyWindows.", "Shutdown scheduled by SleepyWindows.");
        }

        public static string HintWindowsScheduler => Pick(
            "Pode fechar o app, o Windows cuida do resto. Salve seus arquivos antes: os programas abertos serão fechados.",
            "You can close the app, Windows takes it from here. Save your work first: open programs will be closed.");

        public static string HintAppTimer => Pick(
            "Deixe o SleepyWindows aberto (pode minimizar). Fechar o app cancela o timer.",
            "Keep SleepyWindows open (minimizing is fine). Closing the app cancels the timer.");

        public static string TargetTime(DateTime localTarget)
        {
            DateTime today = DateTime.Now.Date;
            string time = localTarget.ToString("t", CultureInfo.CurrentCulture);

            if (localTarget.Date == today)
            {
                return Pick("às " + time, "at " + time);
            }
            if (localTarget.Date == today.AddDays(1))
            {
                return Pick("amanhã às " + time, "tomorrow at " + time);
            }
            return Pick(
                localTarget.ToString("ddd, d 'de' MMM", CultureInfo.CurrentCulture) + " às " + time,
                localTarget.ToString("ddd, MMM d", CultureInfo.InvariantCulture) + " at " + time);
        }

        public static string InvalidNumber => Pick(
            "Digite um número inteiro maior que zero.",
            "Enter a whole number greater than zero.");

        public static string TooLong => Pick(
            "Escolha um tempo de até 999 horas.",
            "Pick a time up to 999 hours.");

        public static string AlreadyScheduled => Pick(
            "Já existe um desligamento ou reinício agendado no Windows.\n\nSubstituir pelo novo?",
            "Windows already has a shutdown or restart scheduled.\n\nReplace it with this one?");

        public static string WindowsRefused(int code)
        {
            return code == PowerControl.ErrorCouldNotRun
                ? Pick("Não foi possível executar o shutdown.exe do Windows.", "Couldn't run Windows' shutdown.exe.")
                : Pick("O Windows recusou o pedido (código " + code + ").", "Windows refused the request (code " + code + ").");
        }

        public static string ExistingCancelled => Pick(
            "Pronto, o desligamento agendado foi cancelado.",
            "Done, the scheduled shutdown was cancelled.");

        public static string NothingToCancel => Pick(
            "Não havia nenhum desligamento agendado.",
            "There was no scheduled shutdown to cancel.");

        public static string CloseCancelsTimer => Pick(
            "Fechar o SleepyWindows cancela o timer.\n\nFechar mesmo assim?",
            "Closing SleepyWindows cancels the timer.\n\nClose anyway?");

        public static string HibernateUnavailable => Pick(
            "A hibernação está desativada neste PC.\n\nPara ativar, abra o Prompt de Comando como administrador e rode: powercfg /h on",
            "Hibernation is turned off on this PC.\n\nTo turn it on, open Command Prompt as administrator and run: powercfg /h on");

        public static string SuspendFailed => Pick(
            "O Windows não conseguiu suspender ou hibernar o PC.",
            "Windows couldn't put the PC to sleep or hibernate it.");
    }
}
