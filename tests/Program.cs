using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SuperNova.ValheimNamekeeper;
using Text = SuperNova.ValheimNamekeeper.PersonalizedText;

internal static class Program
{
    private static int passed;
    private static void Equal<T>(T expected, T actual)
    { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}"); }
    private static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
    private static void Near(float expected, float actual)
    { if (Math.Abs(expected - actual) > .01f) throw new Exception($"Expected approximately {expected}; got {actual}"); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static void Run(string name, Action test)
    { test(); passed++; Console.WriteLine("PASS " + name); }
    private static Dictionary<string, string> Both() => new Dictionary<string, string>
    {
        [Text.NexusNameKey] = "Nexus axe", [Text.NexusDescriptionKey] = "Nexus story",
        [Text.NameKey] = "Keeper axe", [Text.DescriptionKey] = "Keeper story", ["other.mod"] = "keep"
    };
    private static ItemDrop.ItemData Coin(int stack, int world = 0) => new ItemDrop.ItemData
    { m_stack = stack, m_worldLevel = world, m_shared = new ItemDrop.ItemData.SharedData { m_name = "$item_coins" } };
    private static Inventory With(params ItemDrop.ItemData[] items)
    { var inventory = new Inventory(); inventory.Items.AddRange(items); return inventory; }

    public static int Main()
    {
        try
        {
            Run("legacy keys and independent instances", () =>
            {
                var data = Both(); var other = new Dictionary<string, string>(data);
                Equal("Keeper axe", Text.Resolve(data, "Axe", false));
                Text.Apply(data, "Axe", "One", false, false);
                Equal("Keeper axe", Text.Resolve(other, "Axe", false)); Equal("keep", data["other.mod"]);
            });
            Run("Nexus-only values remain visible until Namekeeper edits", () =>
            {
                var data = new Dictionary<string, string> { [Text.NexusNameKey] = "Nexus axe" };
                Equal("Nexus axe", Text.Resolve(data, "Axe", false)); Check(!Text.Owns(data, false));
                Check(!Text.Changes(data, "Axe", "Nexus axe", false, false));
            });
            Run("paid name reset masks Nexus without deleting its data", () =>
            {
                var data = Both(); Check(Text.Changes(data, "Axe", "ignored", true, false));
                Text.Apply(data, "Axe", "ignored", true, false);
                Equal("Axe", Text.Resolve(data, "Axe", false)); Equal("Nexus axe", data[Text.NexusNameKey]);
                Equal("Keeper story", Text.Resolve(data, "Story", true)); Equal("keep", data["other.mod"]);
                Check(!data.ContainsKey(Text.NameKey));
            });
            Run("reset survives reload and locale change", () =>
            {
                var data = Both(); Text.Apply(data, "Axe", "ignored", true, false);
                var loaded = new Dictionary<string, string>(data);
                Equal("Топор", Text.Resolve(loaded, "Топор", false));
                Check(!Text.Changes(loaded, "Топор", "ignored", true, false));
            });
            Run("long original with rich text can be reset without truncation", () =>
            {
                string original = "<color=gold>" + new string('Ж', 900) + "</color>";
                var data = Both(); Text.Apply(data, original, "", true, true);
                Equal(original, Text.Resolve(data, original, true)); Equal("Nexus story", data[Text.NexusDescriptionKey]);
            });
            Run("editing after reset clears only that reset marker", () =>
            {
                var data = Both(); Text.Apply(data, "Axe", "", true, false); Text.Apply(data, "Story", "", true, true);
                Text.Apply(data, "Axe", "New", false, false);
                Equal("New", Text.Resolve(data, "Axe", false)); Check(!data.ContainsKey(Text.ResetNameKey));
                Equal("Story", Text.Resolve(data, "Story", true));
            });
            Run("no-op and independent 50/50/100 fees", () =>
            {
                var data = Both();
                int Fee(string name, string description, bool resetName = false, bool resetDescription = false) =>
                    (Text.Changes(data, "Axe", name, resetName, false) ? 50 : 0) +
                    (Text.Changes(data, "Story", description, resetDescription, true) ? 50 : 0);
                Equal(0, Fee("Keeper axe", "Keeper story")); Equal(50, Fee("New", "Keeper story"));
                Equal(50, Fee("Keeper axe", "New")); Equal(100, Fee("New", "New"));
                Equal(100, Fee("", "", true, true));
                Check(!Text.Changes(null, "Axe", "ignored", true, false));
            });
            Run("empty custom description stays distinct from reset", () =>
            {
                var data = Both(); Text.Apply(data, "Story", "", false, true);
                Equal("", Text.Resolve(data, "Story", true)); Check(Text.Owns(data, true));
                Check(Text.Changes(data, "Story", "", true, true));
            });
            Run("description replacement preserves stats and avoids Nexus duplicates", () =>
            {
                const string stats = "\n\nDamage: 30\nQuality: 3";
                Equal("Keeper story" + stats, Text.ReplaceDescription("Nexus story" + stats,
                    "Keeper story", "$story", "Story", "Nexus story"));
                Equal("Story" + stats, Text.ReplaceDescription("Nexus story" + stats,
                    "Story", "$story", "Story", "Nexus story"));
                Equal("New" + stats, Text.ReplaceDescription("Story extended" + stats, "New", "Story", "Story extended"));
            });
            Run("unrecognized description layout is preserved", () =>
            {
                const string body = "Other mod paragraph\n\nDamage: 30";
                Equal(body, Text.ReplaceDescription(body, "New", "Story"));
            });
            Run("empty original supports descriptions and Nexus reset", () =>
            {
                Equal("Custom\n\nDamage: 30", Text.ReplaceDescription("Damage: 30", "Custom", "", ""));
                Equal("Damage: 30", Text.ReplaceDescription("Nexus story\nDamage: 30", "", "", "", "Nexus story"));
                Equal("Damage: 30", Text.ReplaceDescription("Damage: 30", "", "", ""));
            });
            Run("ground names preserve instructions and handle overlapping names", () =>
            {
                Equal("Nexus\n[E] Pick up", Text.ReplaceHoverName("Nexus axe\n[E] Pick up", "Nexus", "Nexus", "Nexus axe", "Axe"));
                Equal("Nexus axe more\n[E] Pick up", Text.ReplaceHoverName("Nexus axe more\n[E] Pick up", "Nexus axe more", "Nexus axe", "Nexus axe more"));
                Equal("Custom (2)\n[E] Pick up", Text.ReplaceHoverName("Axe (2)\n[E] Pick up", "Custom", "Axe"));
            });
            Run("visible tooltip refresh survives repeated vanilla updates", () =>
            {
                var tooltip = new Tooltip();
                for (int i = 0; i < 4; i++)
                {
                    tooltip.Set("Axe", "Damage: 30", tooltip.m_anchor, tooltip.m_fixedPosition);
                    TooltipTitleUpdater.SetTitle(tooltip, "Custom axe");
                    Equal("Custom axe", tooltip.Displayed); Equal("Damage: 30", tooltip.m_text);
                }
                TooltipTitleUpdater.SetTitle(tooltip, "Axe"); Equal("Axe", tooltip.Displayed);
            });
            Run("tooltip preserves anchor, fixed position and body", () =>
            {
                var tooltip = new Tooltip(); object anchor = tooltip.m_anchor;
                tooltip.m_text = "Story\nDamage: 30"; TooltipTitleUpdater.SetTitle(tooltip, "Name");
                Check(ReferenceEquals(anchor, tooltip.m_anchor)); Equal(7, tooltip.m_fixedPosition.X);
                Equal("Story\nDamage: 30", tooltip.m_text);
                Throws<MissingFieldException>(() => TooltipTitleUpdater.SetTitle(new object(), "Name"));
            });
            Run("current Changed API receives vanilla default arguments", () =>
            {
                var inventory = new CurrentNotifier(); InventoryChangeDispatcher.Notify(inventory);
                Equal(1, inventory.Calls); Check(!inventory.Success && !inventory.Cheated);
            });
            Run("legacy Changed and delegate fallbacks remain available", () =>
            {
                var legacy = new LegacyNotifier(); InventoryChangeDispatcher.Notify(legacy); Equal(1, legacy.Calls);
                var fallback = new DelegateNotifier(); int calls = 0; fallback.m_onChanged = () => calls++;
                InventoryChangeDispatcher.Notify(fallback); Equal(1, calls);
                Throws<MissingMethodException>(() => InventoryChangeDispatcher.Notify(new object()));
            });
            Run("observer errors remain post-commit errors", () =>
            {
                var target = new ItemDrop.ItemData(); var coin = Coin(50); var inventory = With(target, coin);
                InventoryTransaction.Execute(inventory, target, 50, () => target.m_customData[Text.NameKey] = "New");
                Throws<TargetInvocationException>(() => InventoryChangeDispatcher.Notify(new ThrowingNotifier()));
                Equal("New", target.m_customData[Text.NameKey]); Check(!inventory.Items.Contains(coin));
            });
            Run("exact payment across multiple stacks preserves unrelated data", () =>
            {
                Game.m_worldLevel = 0; var target = new ItemDrop.ItemData(); var first = Coin(30); var second = Coin(40);
                first.m_customData["coin.mod"] = "keep"; var inventory = With(target, first, second);
                InventoryTransaction.Execute(inventory, target, 50, () => target.m_customData[Text.NameKey] = "New");
                Check(!inventory.Items.Contains(first)); Equal(20, second.m_stack); Equal(3, target.m_quality);
                Equal(41.5f, target.m_durability); Equal(2, target.m_variant); Equal("keep", first.m_customData["coin.mod"]);
            });
            Run("insufficient money mutates neither item nor coin stacks", () =>
            {
                var target = new ItemDrop.ItemData(); var coin = Coin(49); var inventory = With(target, coin);
                Throws<InvalidOperationException>(() => InventoryTransaction.Execute(inventory, target, 50,
                    () => throw new Exception("Must never run")));
                Equal(49, coin.m_stack); Equal(0, target.m_customData.Count);
            });
            Run("fault after debit restores same coins, order and dictionary reference", () =>
            {
                var target = new ItemDrop.ItemData { m_customData = Both() }; var data = target.m_customData;
                var a = Coin(30); var b = Coin(40); var other = new ItemDrop.ItemData(); var inventory = With(a, target, other, b);
                Throws<InvalidOperationException>(() => InventoryTransaction.Execute(inventory, target, 50, () =>
                { Text.Apply(target.m_customData, "Axe", "New", false, false); throw new InvalidOperationException("Injected"); }));
                Check(inventory.Items.SequenceEqual(new[] { a, target, other, b })); Equal(30, a.m_stack); Equal(40, b.m_stack);
                Check(ReferenceEquals(data, target.m_customData)); Equal("Keeper axe", data[Text.NameKey]);
                Equal("Nexus story", data[Text.NexusDescriptionKey]); Equal("keep", data["other.mod"]);
            });
            Run("null customData and fully removed coin stack recover without prefab", () =>
            {
                var target = new ItemDrop.ItemData { m_customData = null }; var coin = Coin(50); var inventory = With(coin, target);
                Throws<InvalidOperationException>(() => InventoryTransaction.Execute(inventory, target, 50, () =>
                { target.m_customData = Both(); throw new InvalidOperationException(); }));
                Check(target.m_customData == null); Check(ReferenceEquals(coin, inventory.Items[0])); Equal(50, coin.m_stack);
            });
            Run("free service works with zero coins", () =>
            {
                var target = new ItemDrop.ItemData(); var inventory = With(target);
                InventoryTransaction.Execute(inventory, target, 0, () => target.m_customData[Text.NameKey] = "Free");
                Equal("Free", target.m_customData[Text.NameKey]); Equal(1, inventory.Items.Count);
            });
            Run("world-level currency rule matches vanilla", () =>
            {
                Game.m_worldLevel = 2; var target = new ItemDrop.ItemData(); var old = Coin(100, 1); var current = Coin(60, 2);
                var inventory = With(target, old, current);
                InventoryTransaction.Execute(inventory, target, 50, () => { });
                Equal(100, old.m_stack); Equal(10, current.m_stack); Game.m_worldLevel = 0;
            });
            Run("missing item and negative fee fail before mutation", () =>
            {
                var target = new ItemDrop.ItemData(); var coin = Coin(100); var inventory = With(coin);
                Throws<InvalidOperationException>(() => InventoryTransaction.Execute(inventory, target, 50, () => { }));
                Throws<ArgumentOutOfRangeException>(() => InventoryTransaction.Execute(inventory, target, -1, () => { }));
                Equal(100, coin.m_stack);
            });
            Run("merchant uses Russian only for the Russian game locale", () =>
            {
                Check(DialogueLines.UseRussian("Russian")); Check(DialogueLines.UseRussian("russian"));
                foreach (string language in new[] { "English", "German", "French", "Japanese", "Spanish", "", null })
                {
                    Check(!DialogueLines.UseRussian(language));
                    Equal(DialogueLines.Get(DialogueCue.Greeting, "English", 0), DialogueLines.Get(DialogueCue.Greeting, language, 0));
                }
                Check(DialogueLines.Get(DialogueCue.Greeting, "Russian", 0) != DialogueLines.Get(DialogueCue.Greeting, "English", 0));
            });
            Run("every merchant action has safe RU and EN dialogue variants", () =>
            {
                foreach (DialogueCue cue in Enum.GetValues(typeof(DialogueCue)))
                {
                    if (cue == DialogueCue.None) continue;
                    foreach (int variant in new[] { 0, 1, 2, 999, -1 })
                    {
                        string ru = DialogueLines.Get(cue, "Russian", variant), en = DialogueLines.Get(cue, "English", variant);
                        Check(!string.IsNullOrWhiteSpace(ru) && !string.IsNullOrWhiteSpace(en)); Check(ru != en);
                        Check(!ru.Contains('<') && !en.Contains('<')); Check(ru.Length < 150 && en.Length < 150);
                    }
                }
                Equal("", DialogueLines.Get(DialogueCue.None, "Russian", 0));
            });
            Run("successful purchase replaces pending routine chatter", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.SelectItem, 0);
                scheduler.Request(DialogueCue.SuccessBoth, .1);
                Check(scheduler.TryTake(.1, out var cue)); Equal(DialogueCue.SuccessBoth, cue);
            });
            Run("errors are not overwritten by idle or selection chatter", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.NotEnoughCoins, 0);
                scheduler.Request(DialogueCue.Idle, .1); scheduler.Request(DialogueCue.SelectItem, .2);
                Check(scheduler.TryTake(.3, out var cue)); Equal(DialogueCue.NotEnoughCoins, cue);
            });
            Run("important outcome waits briefly before interrupting a current line", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.Greeting, 0);
                Check(scheduler.TryTake(0, out _)); scheduler.Request(DialogueCue.SuccessName, .1);
                Check(!scheduler.TryTake(1, out _)); Check(scheduler.TryTake(1.5, out var cue));
                Equal(DialogueCue.SuccessName, cue);
            });
            Run("repeated errors do not spam text or audio", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.InvalidText, 0);
                Check(scheduler.TryTake(0, out _)); scheduler.Request(DialogueCue.InvalidText, 2);
                Check(!scheduler.TryTake(5, out _)); scheduler.Request(DialogueCue.InvalidText, 8);
                Check(scheduler.TryTake(8, out _));
            });
            Run("routine clicks cannot queue behind a fresh purchase result", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.SuccessName, 0);
                Check(scheduler.TryTake(0, out _)); scheduler.Request(DialogueCue.SelectItem, 1);
                Check(!scheduler.TryTake(6, out _));
            });
            Run("stale pending lines expire rather than speaking after the player returns", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.Open, 0);
                Check(!scheduler.TryTake(9, out _)); Equal(0, scheduler.PendingPriority);
                scheduler.Request(DialogueCue.Greeting, 10); scheduler.Clear(); Check(!scheduler.TryTake(10, out _));
            });
            Run("idle lines have a longer repetition interval", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.Idle, 0);
                Check(scheduler.TryTake(0, out _)); scheduler.Request(DialogueCue.Idle, 20);
                Check(!scheduler.TryTake(20, out _)); scheduler.Request(DialogueCue.Idle, 30);
                Check(scheduler.TryTake(30, out _));
            });
            Run("native HUD fits desktop and low-resolution canvas sizes", () =>
            {
                foreach (var size in new[] { (1920f,1080f), (2560f,1440f), (1280f,720f), (1024f,768f), (800f,600f), (640f,480f) })
                {
                    var layout = new HudLayout(size.Item1, size.Item2);
                    Check(layout.Width * layout.Scale <= size.Item1 - 47.9f);
                    Check(layout.Height * layout.Scale <= size.Item2 - 63.9f);
                    Check(layout.Scale > 0 && layout.Scale <= 1);
                    Check(layout.Height - 132 - 194 > 0); // Inventory viewport remains positive.
                    Check(layout.Height - 132 - 158 > 0); // Editor scroll viewport remains positive.
                }
            });
            Run("lower resolutions do not increase the HUD share of the screen", () =>
            {
                foreach (var size in new[] { (1920f,1080f), (1600f,900f), (1280f,720f), (1024f,768f), (800f,600f), (640f,480f) })
                {
                    var layout = new HudLayout(size.Item1, size.Item2);
                    Check(layout.Width * layout.Scale / size.Item1 <= .49f + .0001f);
                    Check(layout.Height * layout.Scale / size.Item2 <= .704f);
                }
            });
            Run("720p scales the complete 1080p panel proportionally", () =>
            {
                var full = new HudLayout(1920, 1080); var small = new HudLayout(1280, 720);
                Near(940, full.Width * full.Scale); Near(760, full.Height * full.Scale);
                Near(940 * 2f / 3, small.Width * small.Scale);
                Near(760 * 2f / 3, small.Height * small.Scale);
                Near(full.Width / full.Height, small.Width / small.Height);
                Near(24, full.LeftInset); Near(16, small.LeftInset);
            });
            Run("repeated resolution changes restore the same HUD dimensions", () =>
            {
                var initial = new HudLayout(1920, 1080);
                for (int i = 0; i < 20; i++)
                {
                    _ = new HudLayout(800, 600); _ = new HudLayout(1280, 720);
                    var restored = new HudLayout(1920, 1080);
                    Near(initial.Width, restored.Width); Near(initial.Height, restored.Height); Near(initial.Scale, restored.Scale);
                }
            });
            Run("ultrawide and large canvases preserve aspect and avoid enlargement", () =>
            {
                foreach (var size in new[] { (3440f,1440f), (3840f,2160f), (2560f,720f), (960f,1080f) })
                {
                    var layout = new HudLayout(size.Item1, size.Item2);
                    Near(940f / 760, layout.Width / layout.Height);
                    Check(layout.Width * layout.Scale <= 940 && layout.Height * layout.Scale <= 760);
                    Check(layout.Width * layout.Scale / size.Item1 <= .49f + .0001f);
                    Check(layout.Height * layout.Scale / size.Item2 <= .704f);
                }
            });
            Run("closing replaces pending edit chatter with a farewell", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.NameEdited, 0);
                scheduler.Request(DialogueCue.Goodbye, .1); Check(scheduler.TryTake(.1, out var cue));
                Equal(DialogueCue.Goodbye, cue);
            });
            Run("closing does not hide a pending purchase outcome", () =>
            {
                var scheduler = new DialogueScheduler(); scheduler.Request(DialogueCue.SuccessBoth, 0);
                scheduler.Request(DialogueCue.Goodbye, .1); Check(scheduler.TryTake(.1, out var cue));
                Equal(DialogueCue.SuccessBoth, cue);
            });
            Run("HUD sizing remains valid during temporarily zero-sized canvas", () =>
            {
                var layout = new HudLayout(0, 0);
                Check(layout.Scale > 0 && !float.IsNaN(layout.Scale));
                Check(layout.Width > 0 && layout.Height > 0);
            });
            Run("workshop base and roof use unique symmetric direct-child anchors", () =>
            {
                var anchors = WorkshopLayout.BodyAnchors().ToArray();
                Equal(19, anchors.Length); Equal(anchors.Length, anchors.Select(a => a.Key).Distinct().Count());
                foreach (var a in anchors)
                {
                    Check(Math.Abs(a.X) <= 2.5f && Math.Abs(a.Z) <= 2);
                    Check(anchors.Any(b => b.X == -a.X && b.Y == a.Y && b.Z == -a.Z));
                }
                Equal(9, anchors.Count(a => a.Y == 0));
            });
            Run("replacement roof anchors match all existing body mounts", () =>
            {
                var body = WorkshopLayout.BodyAnchors().ToArray();
                foreach (var roof in WorkshopLayout.RoofAnchors())
                {
                    var mount = body.Single(a => a.Key == "mount_" + roof.Key);
                    Near(roof.X, mount.X); Near(roof.Z, mount.Z); Near(WorkshopLayout.RoofHeight, mount.Y - roof.Y);
                }
            });
            Run("legacy roof migration runs once across reloads and ownership changes", () =>
            {
                int state = 0, roofs = 0;
                for (int i = 0; i < 10; i++)
                    RoofMigration.Initialize(state, true, true, roofs > 0, x => state = x, () => roofs++);
                Equal(2, state); Equal(1, roofs);
                // A destroyed roof stays absent; only manual building can replace it.
                RoofMigration.Initialize(state, true, true, false, x => state = x, () => roofs++);
                Equal(1, roofs);
            });
            Run("clients and missing prefab cannot reserve or spawn a roof", () =>
            {
                foreach (var conditions in new[] { (false, true), (true, false), (false, false) })
                    RoofMigration.Initialize(0, conditions.Item1, conditions.Item2, false,
                        _ => throw new Exception("Unexpected persistent write"), () => throw new Exception("Unexpected roof"));
            });
            Run("recorded existing roof is adopted without creating a duplicate", () =>
            {
                int state = 0;
                RoofMigration.Initialize(state, true, true, true, x => state = x, () => throw new Exception("Duplicate"));
                Equal(2, state);
            });
            Run("failure before roof creation stays reserved and cannot retry after reload", () =>
            {
                int state = 0, calls = 0;
                Throws<InvalidOperationException>(() => RoofMigration.Initialize(state, true, true, false,
                    x => state = x, () => { calls++; throw new InvalidOperationException("Injected creation failure"); }));
                Equal(1, state);
                RoofMigration.Initialize(state, true, true, false, x => state = x, () => calls++);
                Equal(1, calls);
            });
            Run("failure completing an existing roof cannot duplicate it", () =>
            {
                int state = 0, roofs = 0;
                Throws<InvalidOperationException>(() => RoofMigration.Initialize(state, true, true, false,
                    x => { if (x == 2) throw new InvalidOperationException("Injected completion failure"); state = x; }, () => roofs++));
                Equal(1, roofs); Equal(1, state);
                RoofMigration.Initialize(state, true, true, true, x => state = x, () => roofs++);
                Equal(1, roofs);
            });
            Run("included roof never adds a free material refund", () =>
            {
                Check(!RoofMigration.CanRefund(true)); Check(RoofMigration.CanRefund(false));
                Equal(4, WorkshopLayout.RoofWood);
                int shopWood = 20;
                Equal(0, -shopWood + shopWood + (RoofMigration.CanRefund(true) ? WorkshopLayout.RoofWood : 0));
                Equal(0, -WorkshopLayout.RoofWood + (RoofMigration.CanRefund(false) ? WorkshopLayout.RoofWood : 0));
            });
            Run("included roof refund cannot mutate shared recipe or another roof instance", () =>
            {
                var wood = new ItemDrop();
                var recipe = new[] { new Piece.Requirement { m_resItem = wood, m_amount = 4, m_recover = true,
                    m_amountPerLevel = 2, m_extraAmountOnlyOneIngredient = 3, m_upgraderResource = true },
                    new Piece.Requirement { m_amount = 1, m_recover = false } };
                var included = RoofRequirements.ForInstance(recipe, true);
                var purchased = RoofRequirements.ForInstance(recipe, false);
                Check(!ReferenceEquals(recipe, included)); Check(!ReferenceEquals(recipe[0], included[0]));
                Check(recipe[0].m_recover && purchased[0].m_recover && !included[0].m_recover);
                Check(!purchased[1].m_recover); Check(ReferenceEquals(wood, included[0].m_resItem));
                Equal(4, included[0].m_amount); Equal(2, included[0].m_amountPerLevel);
                Equal(3, included[0].m_extraAmountOnlyOneIngredient); Check(included[0].m_upgraderResource);
                included[0].m_amount = 99; Equal(4, recipe[0].m_amount); Equal(4, purchased[0].m_amount);
            });
            Run("server recipe sync changes amounts and preserves resource references", () =>
            {
                var wood = new ItemDrop();
                var stone = new ItemDrop();
                var woodRequirement = new Piece.Requirement { m_resItem = wood, m_amount = 20, m_amountPerLevel = 2, m_recover = true };
                var stoneRequirement = new Piece.Requirement { m_resItem = stone, m_amount = 10, m_amountPerLevel = 1 };
                var requirements = new[] { woodRequirement, stoneRequirement };

                Check(RecipeRequirementAmounts.TryApply(requirements, new[] { 24, 12 }));
                Equal(24, woodRequirement.m_amount); Equal(12, stoneRequirement.m_amount);
                Check(ReferenceEquals(wood, woodRequirement.m_resItem)); Check(ReferenceEquals(stone, stoneRequirement.m_resItem));
                Equal(2, woodRequirement.m_amountPerLevel); Check(woodRequirement.m_recover);
            });
            Run("incomplete server recipe sync is atomic", () =>
            {
                var woodRequirement = new Piece.Requirement { m_resItem = new ItemDrop(), m_amount = 20 };
                var invalidRequirement = new Piece.Requirement { m_resItem = null, m_amount = 10 };
                var requirements = new[] { woodRequirement, invalidRequirement };

                Check(!RecipeRequirementAmounts.TryApply(requirements, new[] { 24, 12 }));
                Equal(20, woodRequirement.m_amount); Equal(10, invalidRequirement.m_amount);
                Check(!RecipeRequirementAmounts.TryApply(requirements, new[] { 24 }));
            });
            Run("all native snapping tokens use valid localization characters", () =>
            {
                foreach (var anchor in WorkshopLayout.BodyAnchors().Concat(WorkshopLayout.RoofAnchors()))
                    Check(System.Text.RegularExpressions.Regex.IsMatch("sn_snap_" + anchor.Key, "^[A-Za-z0-9_]+$"));
            });
            Run("foot contact anchors match the approved workshop floor and roof", () =>
            {
                var anchors = WorkshopLayout.BodyAnchors().ToArray();
                var feet = anchors.Where(a => a.Key.StartsWith("foundation_")).ToArray();
                Equal(5, feet.Length); Check(feet.All(a => a.Y == -.58f));
                Equal(9, anchors.Count(a => a.Y == 0));
                Equal(5, anchors.Count(a => a.Y == WorkshopLayout.RoofHeight));
                Equal(4, feet.Count(a => Math.Abs(a.X) == 2.05f && Math.Abs(a.Z) == 1.64f));
            });
            Run("native lantern range has useful coverage and a bounded maximum", () =>
            {
                Near(6, LightingRules.Range(0)); Near(6, LightingRules.Range(3.2f));
                Near(7, LightingRules.Range(7)); Near(8, LightingRules.Range(100));
            });
            Run("native lantern intensity is brighter with a safe fallback", () =>
            {
                Near(2.5f, LightingRules.Intensity(0)); Near(2.5f, LightingRules.Intensity(1.05f));
                Near(3, LightingRules.Intensity(3)); Near(4, LightingRules.Intensity(100));
            });
            Console.WriteLine($"{passed} tests passed; game runtime not exercised."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    internal struct Position { public int X; }
    internal sealed class Tooltip
    {
        public string m_topic = "", m_text = "";
        public object m_anchor = new object(); public Position m_fixedPosition = new Position { X = 7 };
        public string Displayed = "";
        public void Set(string topic, string body, object anchor, Position position)
        { m_topic = topic; m_text = body; m_anchor = anchor; m_fixedPosition = position; Displayed = topic; }
    }
    internal sealed class CurrentNotifier
    {
        public int Calls; public bool Success = true, Cheated = true;
        private void Changed(bool success = false, bool cheatedStateChanged = false)
        { Calls++; Success = success; Cheated = cheatedStateChanged; }
    }
    internal sealed class LegacyNotifier { public int Calls; private void Changed() { Calls++; } }
    internal sealed class DelegateNotifier { public Action m_onChanged; }
    internal sealed class ThrowingNotifier { private void Changed(bool a, bool b) => throw new Exception("Observer failure"); }
}
