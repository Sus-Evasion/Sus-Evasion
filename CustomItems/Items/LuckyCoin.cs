using System.Collections.Generic;
using CustomItems.API;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using UnityEngine;

namespace CustomItems.Items
{
    public class LuckyCoin : CustomItem
    {
        public LuckyCoin(ItemType type, int itemId) : base(type, itemId)
        {
        }

        public override string Name { get; set; } = Plugin.Singleton.Config.ItemConfigs.LuckyCfg.Name;
        public override Dictionary<SpawnLocation, float> SpawnLocations { get; set; } = Plugin.Singleton.Config.ItemConfigs.LuckyCfg.SpawnLocations;
        public override string Description { get; set; } = Plugin.Singleton.Config.ItemConfigs.LuckyCfg.Description;
        public override int SpawnLimit { get; set; } = Plugin.Singleton.Config.ItemConfigs.LuckyCfg.SpawnLimit;

        protected override void LoadEvents()
        {
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayersLC;
            Exiled.Events.Handlers.Player.EnteringPocketDimension += OnEnterPocketDimension;
            base.LoadEvents();
        }

        protected override void UnloadEvents()
        {
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayersLC;
            Exiled.Events.Handlers.Player.EnteringPocketDimension -= OnEnterPocketDimension;
            base.UnloadEvents();
        }

        private List<PocketDimensionTeleport> teleports = new List<PocketDimensionTeleport>();
        private bool isDropped = false;
        private bool onCooldown = false;
        
        private void OnWaitingForPlayersLC()
        {
            foreach (PocketDimensionTeleport teleport in Object.FindObjectsOfType<PocketDimensionTeleport>())
                teleports.Add(teleport);
        }

        protected override void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (CheckItem(ev.Item))
            {
                if (ev.Player.CurrentRoom.Name == "PocketWorld")
                {
                    ev.IsAllowed = false;
                    Log.Debug($"{Name} has been dropped in the Pocket Dimension.", Plugin.Singleton.Config.Debug);
                    isDropped = true;
                    ev.Player.RemoveItem(ev.Item);
                }
                else
                    base.OnDroppingItem(ev);
            }
        }
        
        private void OnEnterPocketDimension(EnteringPocketDimensionEventArgs ev)
        {
            Log.Debug($"{ev.Player.Nickname} Entering Pocket Dimension.", Plugin.Singleton.Config.Debug);
            if (onCooldown)
            {
                Log.Debug($"{ev.Player.Nickname} - Not spawning, on cooldown.", Plugin.Singleton.Config.Debug);
                return;
            }
            
            if (isDropped && ev.Position.y < -1900f)
            {
                Log.Debug($"{ev.Player.Nickname} - EPD checks passed.", Plugin.Singleton.Config.Debug);
                foreach (PocketDimensionTeleport teleport in teleports)
                {
                    Log.Debug($"{ev.Player.Nickname} - Checking teleporter..", Plugin.Singleton.Config.Debug);
                    if (teleport.type == PocketDimensionTeleport.PDTeleportType.Exit)
                    {
                        onCooldown = true;
                        Log.Debug($"{ev.Player.Nickname} - Valid exit found..", Plugin.Singleton.Config.Debug);
                        Vector3 tpPos = teleport.transform.position;
                        float dist = Vector3.Distance(tpPos, ev.Position);
                        Vector3 spawnPos = Vector3.MoveTowards(tpPos, ev.Position, dist / 2);
                        Log.Debug($"{ev.Player.Nickname} - TP: {tpPos}, Dist: {dist}, Spawn: {spawnPos}", Plugin.Singleton.Config.Debug);

                        Pickup coin = Exiled.API.Extensions.Item.Spawn(ItemType.Coin, 0f, spawnPos);

                        Timing.CallDelayed(2f, () => coin.Delete());
                        Timing.CallDelayed(120f, () => onCooldown = false);
                    }
                }
            }
        }
    }
}