using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;

namespace ICaughtThis
{
    public class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();

            Helper.Events.GameLoop.GameLaunched += (e, a) => OnGameLaunched(e, a);
            Helper.Events.Input.ButtonPressed += (e, a) => OnButtonPressed(e, a);
        }

        /// <summary>Add to Generic Mod Config Menu</summary>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add config options
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Options_ClaimCreditKey"),
                getValue: () => this.Config.ClaimCreditKey,
                setValue: value => this.Config.ClaimCreditKey = value
            );
        }

        /// <summary>React to action key</summary>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button != this.Config.ClaimCreditKey)
            {
                return;
            }

            // What item is current player holding?

            if (Game1.player == null)
            {
                this.Monitor.Log("[I Caught This] No current player", LogLevel.Trace);
                return;
            }

            if (Game1.player.CurrentItem == null || Game1.player.CurrentItem.itemId == null)
            {
                this.Monitor.Log("[I Caught This] No current item", LogLevel.Trace);
                Game1.drawDialogueNoTyping(Helper.Translation.Get("Response_NotHoldingAnything"));
                return;
            }

            var itemId = Game1.player.CurrentItem.itemId.ToString();
            var itemName = Game1.player.CurrentItem.DisplayName;
            this.Monitor.Log($"[I Caught This] Item ID = {itemId}, name = {itemName}", LogLevel.Trace);

            var metadata = ItemRegistry.GetMetadata(itemId);
            itemId = metadata.QualifiedItemId;
            this.Monitor.Log($"[I Caught This] Qualified item ID = {itemId}", LogLevel.Trace);

            // Perform same sanity checks as base game caughtFish()

            if (!metadata.Exists())
            {
                this.Monitor.Log("[I Caught This] {itemId} has no metadata", LogLevel.Trace);
                Game1.drawDialogueNoTyping(Helper.Translation.Get("Response_NotHoldingAnything"));
                return;
            }

            if (ItemContextTagManager.HasBaseTag(metadata.QualifiedItemId, "trash_item"))
            {
                this.Monitor.Log($"[I Caught This] {itemName} is trash", LogLevel.Trace);
                Game1.drawDialogueNoTyping(Helper.Translation.Get("Response_NotHoldingFish", new { itemName = itemName }));
                return;
            }

            if (itemId == "(O)167")
            {
                this.Monitor.Log($"[I Caught This] {itemName} is Joja Cola", LogLevel.Trace);
                Game1.drawDialogueNoTyping(Helper.Translation.Get("Response_NotHoldingFish", new { itemName = itemName }));
                return;
            }

            var isFish = false;
            if (metadata.GetParsedData()?.ObjectType == "Fish")
            {
                isFish = true;
            }
            else if (itemId == "(O)372") // Clam
            {
                isFish = true;
            }

            if (!isFish)
            {
                this.Monitor.Log($"[I Caught This] {itemName} isn't fish", LogLevel.Trace);
                Game1.drawDialogueNoTyping(Helper.Translation.Get("Response_NotHoldingFish", new { itemName = itemName }));
                return;
            }

            // Do they already have credit for catching it?

            if (Game1.player.fishCaught.ContainsKey(itemId))
            {
                this.Monitor.Log($"[I Caught This] Already have credit for catching {itemName}", LogLevel.Trace);
                Game1.drawDialogueNoTyping(Helper.Translation.Get("Response_AlreadyCredited", new { itemName = itemName }));
                return;
            }

            // Give them credit

            this.Monitor.Log($"[I Caught This] About to claim credit for catching {itemName}", LogLevel.Debug);
            Game1.player.caughtFish(
                itemId: itemId,
                size: 9 // same as base game's DebugCommands.cs CatchAllFish()
            );
            this.Monitor.Log($"[I Caught This] Claimed credit for catching {itemName}", LogLevel.Debug);
            Game1.drawDialogueNoTyping(Helper.Translation.Get("Response_ClaimedCredit", new { itemName = itemName }));
        }
    }
}
