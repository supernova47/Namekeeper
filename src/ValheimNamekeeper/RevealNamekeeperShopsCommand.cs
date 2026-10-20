using System;
using System.Collections.Generic;
using Jotunn.Entities;

namespace SuperNova.ValheimNamekeeper
{
    internal sealed class RevealNamekeeperShopsCommand : ConsoleCommand
    {
        public override string Name => "namekeeper_map";

        public override string Help => L.T(
            "namekeeper_map <on|off> — admin: request or remove Namekeeper shop markers on your map",
            "namekeeper_map <on|off> — администратор: показать или скрыть метки лавок на своей карте");

        public override List<string> CommandOptionList() => new List<string> { "on", "off" };

        public override void Run(string[] args)
        {
            RunCommand(args, null);
        }

        public override void Run(string[] args, Terminal context)
        {
            RunCommand(args, context);
        }

        private static void RunCommand(string[] args, Terminal context)
        {
            if (args == null || args.Length != 1)
            {
                Reply(context, L.T(
                    "Usage: namekeeper_map <on|off>. Current state: " + (ShopWorldLocations.MapMarkersVisible ? "on" : "off"),
                    "Использование: namekeeper_map <on|off>. Сейчас: " + (ShopWorldLocations.MapMarkersVisible ? "включено" : "выключено")));
                return;
            }

            bool visible;
            if (string.Equals(args[0], "on", StringComparison.OrdinalIgnoreCase)) visible = true;
            else if (string.Equals(args[0], "off", StringComparison.OrdinalIgnoreCase)) visible = false;
            else
            {
                Reply(context, L.T("Usage: namekeeper_map <on|off>.",
                    "Использование: namekeeper_map <on|off>."));
                return;
            }

            int shown = ShopWorldLocations.SetMapMarkersVisible(visible);
            if (!visible)
            {
                Reply(context, L.T("Temporary Namekeeper shop markers were removed from your map.",
                    "Временные метки лавок имён удалены с твоей карты."));
                return;
            }

            if (ShopWorldLocations.MapRequestPending)
            {
                Reply(context, L.T("Requesting shop locations from the server. Markers will appear after its reply.",
                    "Запрашиваю координаты лавок у сервера. Метки появятся после ответа."));
            }
            else if (!ShopWorldLocations.MapRevealRpcReady)
            {
                Reply(context, L.T("The map request is not ready yet. Wait until you are fully in the world, then try again.",
                    "Запрос карты пока не готов. Дождись полной загрузки мира и повтори команду."));
            }
            else if (shown == 0)
            {
                Reply(context, L.T("No generated Namekeeper shop locations were found in this world.",
                    "В этом мире пока не найдены сгенерированные лавки имён."));
            }
            else
            {
                Reply(context, L.T("Showing " + shown + " of 3 Namekeeper shop markers on your map.",
                    "На твоей карте показано меток лавок: " + shown + " из 3."));
            }
        }

        private static void Reply(Terminal context, string message)
        {
            if (context != null) context.AddString(message);
            else if (global::Console.instance != null) global::Console.instance.Print(message);
        }
    }
}
