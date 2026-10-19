using System;
using System.Collections.Generic;

namespace SuperNova.ValheimNamekeeper
{
    internal enum DialogueCue
    {
        None, Arrival, Greeting, Open, SelectItem, NameEdited, DescriptionEdited,
        ResetName, ResetDescription, CancelReset, SuccessName, SuccessDescription,
        SuccessBoth, SuccessReset, NoChanges, NotEnoughCoins, InvalidText,
        ItemUnavailable, NoEligibleItems, NoRoof, TooFar, Failure, Goodbye, Idle
    }

    internal static class DialogueLines
    {
        private sealed class Line
        {
            internal readonly string English, Russian;
            internal Line(string english, string russian) { English = english; Russian = russian; }
        }
        private static readonly Dictionary<DialogueCue, Line[]> Lines = new Dictionary<DialogueCue, Line[]>
        {
            [DialogueCue.Arrival] = new[] {
                new Line("The shop is open. Every treasure has a story.", "Лавка открыта. У каждой вещи есть своя история."),
                new Line("My counter is ready, and so are my quills.", "Прилавок готов, перья заточены. Заходи!") },
            [DialogueCue.Greeting] = new[] {
                new Line("Welcome, traveller. What tale have you brought me?", "Здравствуй, путник. Какую историю ты мне принёс?"),
                new Line("Come closer. Even an old axe deserves a fine name.", "Подходи. Даже старый топор достоин славного имени.") },
            [DialogueCue.Open] = new[] {
                new Line("Choose a piece of gear. We shall give it a story.", "Выбирай вещь. Найдём для неё достойную историю."),
                new Line("A name, a tale, or both? The choice is yours.", "Имя, история или всё сразу? Решать тебе.") },
            [DialogueCue.SelectItem] = new[] {
                new Line("A worthy choice. What shall we call it?", "Достойный выбор. Как мы её назовём?"),
                new Line("This one has seen a few adventures, I think.", "Кажется, эта вещь повидала немало приключений.") },
            [DialogueCue.NameEdited] = new[] {
                new Line("A good name should be remembered.", "Хорошее имя должно запоминаться."),
                new Line("Try the name aloud. Does it suit your treasure?", "Произнеси это имя. Подходит твоему сокровищу?") },
            [DialogueCue.DescriptionEdited] = new[] {
                new Line("A tale worth keeping. Check the words before we begin.", "Такую историю стоит сохранить. Проверь слова перед работой."),
                new Line("A few words can hold a whole adventure.", "В нескольких словах может уместиться целое приключение.") },
            [DialogueCue.ResetName] = new[] {
                new Line("Its first name, then. Restoration costs the usual fee.", "Значит, вернём прежнее имя. Цена — как за новое."),
                new Line("Some names are best left as they were.", "Иные имена хороши такими, какими были.") },
            [DialogueCue.ResetDescription] = new[] {
                new Line("We can restore the old tale. Check the price below.", "Можно вернуть прежнюю историю. Цена указана внизу."),
                new Line("Back to the beginning of its story, then.", "Что ж, вернёмся к началу её истории.") },
            [DialogueCue.CancelReset] = new[] {
                new Line("As you wish. Nothing has been changed or charged.", "Как пожелаешь. Ничего не изменено и не оплачено.") },
            [DialogueCue.SuccessName] = new[] {
                new Line("There. May its new name bring you glory!", "Готово. Пусть новое имя принесёт тебе славу!"),
                new Line("A fine name for a fine piece of gear.", "Славное имя для славной вещи.") },
            [DialogueCue.SuccessDescription] = new[] {
                new Line("Its story is written. May the next chapter be yours.", "История записана. Пусть следующая глава будет твоей."),
                new Line("Done. Those words belong to this treasure now.", "Готово. Теперь эти слова принадлежат твоему сокровищу.") },
            [DialogueCue.SuccessBoth] = new[] {
                new Line("A new name and a new tale. Carry them with pride!", "Новое имя и новая история. Носи их с гордостью!"),
                new Line("All finished. Now go and make that story longer.", "Всё готово. А теперь отправляйся за продолжением истории.") },
            [DialogueCue.SuccessReset] = new[] {
                new Line("The original words are restored. Just as you asked.", "Прежние слова возвращены. Всё, как ты просил."),
                new Line("Restored. Sometimes the first story is the best.", "Восстановлено. Иногда первая история — самая лучшая.") },
            [DialogueCue.NoChanges] = new[] {
                new Line("The words are the same. Keep your coins.", "Слова остались прежними. Побереги монеты.") },
            [DialogueCue.NotEnoughCoins] = new[] {
                new Line("A few more coins, traveller. Good work takes time.", "Нужно чуть больше монет, путник. Добрая работа требует времени."),
                new Line("Your purse is a little light. I can wait.", "Кошель немного легковат. Ничего, я подожду.") },
            [DialogueCue.InvalidText] = new[] {
                new Line("Check those words: keep them within the limit, without special markings.", "Проверь текст: уложись в меру и убери особые знаки."),
                new Line("A little shorter and clearer, if you please.", "Чуть короче и яснее, будь добр.") },
            [DialogueCue.ItemUnavailable] = new[] {
                new Line("Keep that item in your pack while I work.", "Держи выбранную вещь в своём инвентаре, пока я работаю.") },
            [DialogueCue.NoEligibleItems] = new[] {
                new Line("Bring a single piece of gear, not a stack of supplies.", "Принеси отдельную вещь, а не стопку припасов.") },
            [DialogueCue.NoRoof] = new[] {
                new Line("I need a roof over my work before I can help.", "Над моей работой нужна крыша. Тогда и помогу."),
                new Line("Let us keep the rain off the ink first.", "Сперва укроем чернила от дождя.") },
            [DialogueCue.TooFar] = new[] {
                new Line("Come back to the counter when you are ready.", "Возвращайся к прилавку, когда будешь готов.") },
            [DialogueCue.Failure] = new[] {
                new Line("That did not go as planned. Let us check it before trying again.", "Что-то пошло не так. Проверим всё перед новой попыткой.") },
            [DialogueCue.Goodbye] = new[] {
                new Line("Safe travels. Bring me another story sometime.", "Счастливого пути. Заглядывай с новыми историями."),
                new Line("May your gear serve you well. Until next time!", "Пусть вещи служат тебе верно. До новой встречи!") },
            [DialogueCue.Idle] = new[] {
                new Line("A sharp blade is useful. A famous blade is remembered.", "Острый клинок полезен. Прославленный — помнят."),
                new Line("Names fade. Stories keep them alive.", "Имена забываются. Истории помогают им жить."),
                new Line("Some treasures are worth more than their weight in silver.", "Иные сокровища стоят дороже своего веса в серебре.") }
        };

        internal static bool UseRussian(string gameLanguage) =>
            string.Equals(gameLanguage, "Russian", StringComparison.OrdinalIgnoreCase);
        internal static string Get(DialogueCue cue, string gameLanguage, int variant)
        {
            if (!Lines.TryGetValue(cue, out Line[] lines)) return "";
            Line line = lines[(int)((uint)variant % (uint)lines.Length)];
            return UseRussian(gameLanguage) ? line.Russian : line.English;
        }
        internal static int Priority(DialogueCue cue)
        {
            if (cue == DialogueCue.None) return 0;
            if (cue == DialogueCue.Arrival || cue == DialogueCue.Greeting || cue == DialogueCue.Goodbye || cue == DialogueCue.Idle) return 1;
            return cue >= DialogueCue.SuccessName && cue <= DialogueCue.Failure ? 3 : 2;
        }
    }
}
