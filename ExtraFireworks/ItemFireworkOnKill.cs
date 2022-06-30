﻿using BepInEx.Configuration;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace ExtraFireworks;

public class ItemFireworkOnKill : FireworkItem
{
    private ConfigurableLinearScaling scaler;
    
    public ItemFireworkOnKill(ExtraFireworks plugin, ConfigFile config) : base(plugin, config)
    {
        scaler = new ConfigurableLinearScaling(config, "", GetConfigSection(), 2, 1);
    }

    public override string GetName()
    {
        return "FireworkOnKill";
    }

    public override string GetPickupModelName()
    {
        return "BottledFireworks.prefab";
    }

    public override string GetPickupIconName()
    {
        return "BottledFireworks.png";
    }

    public override ItemTiers GetTier()
    {
        return ItemTiers.Green;
    }

    public override float GetModelScale()
    {
        return .6f;
    }

    public override string GetItemName()
    {
        return "Will-o'-the-Firework";
    }

    public override string GetItemPickup()
    {
        return "Spawn fireworks on kill";
    }

    public override string GetItemDescription()
    {
        return $"Whenever you <style=cIsDamage>kill an enemy</style>, it explodes into a barrage of {scaler.Base} fireworks <style=cStack>(+{scaler.Scaling} per stack)</style>.";
    }

    public override string GetItemLore()
    {
        return "Revolutionary design.";
    }

    public override void AddHooks()
    {
        // Implement fireworks on kill
        GlobalEventManager.onCharacterDeathGlobal += (report) =>
        {
            if (!report.attacker || !report.attackerBody)
                return;

            var attackerCharacterBody = report.attackerBody;
            if (attackerCharacterBody.inventory)
            {
                var count = attackerCharacterBody.inventory.GetItemCount(Item);
                if (count > 0)
                    ExtraFireworks.SpawnFireworks(report.victim.body.coreTransform, attackerCharacterBody, scaler.GetValueInt(count), false);
            }
        };
    }
}