﻿using BepInEx.Configuration;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace ExtraFireworks;

public class ItemFireworkOnHit : FireworkItem
{
    private ConfigurableLinearScaling scaler;
    private ConfigEntry<int> numFireworks;

    private static readonly float MAX_FIREWORK_HEIGHT = 50f;
    
    public ItemFireworkOnHit(ExtraFireworks plugin, ConfigFile config) : base(plugin, config)
    {
        numFireworks = config.Bind(GetConfigSection(), "FireworksPerHit", 1, "Number of fireworks per hit");
        scaler = new ConfigurableLinearScaling(config, "", GetConfigSection(), 10, 10);
    }

    public override string GetName()
    {
        return "FireworkOnHit";
    }

    public override string GetPickupModelName()
    {
        return "Firework Dagger.prefab";
    }
    
    public override float GetModelScale()
    {
        return 0.15f;
    }

    public override string GetPickupIconName()
    {
        return "FireworkDagger.png";
    }

    public override ItemTier GetTier()
    {
        return ItemTier.Tier1;
    }
    
    public override ItemTag[] GetTags()
    {
        return new[] { ItemTag.Damage, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };
    }
    
    public override string GetItemName()
    {
        return "Firework Dagger";
    }

    public override string GetItemPickup()
    {
        return "Chance to fire fireworks on hit";
    }

    public override string GetItemDescription()
    {
        var desc = $"Gain a <style=cIsDamage>{scaler.Base:0}%</style> chance " +
                   $"<style=cStack>(+{scaler.Scaling:0}% per stack)</style> <style=cIsDamage>on hit</style> to ";

        if (numFireworks.Value == 1)
            desc += "<style=cIsDamage>fire a firework</style> for <style=cIsDamage>300%</style> base damage.";
        else
            desc += $"<style=cIsDamage>fire {numFireworks.Value} fireworks</style> for <style=cIsDamage>300%</style> " +
                    $"base damage each.";
        
        return desc;
    }

    public override string GetItemLore()
    {
        return "You got stabbed by a firework and is kill.";
    }

    public override void AddHooks()
    {
        // Implement fireworks on hit
        On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
        {
            if (damageInfo.procCoefficient == 0f || damageInfo.rejected || !NetworkServer.active)
                goto end;

            // Check to make sure fireworks don't proc themselves
            if (damageInfo.procChainMask.HasProc(ProcType.MicroMissile))
                goto end;

            // Fireworks can't proc themselves even if outside the proc chain
            if (damageInfo.inflictor && damageInfo.inflictor.GetComponent<MissileController>())
                goto end;

            if (!damageInfo.attacker)
                goto end;
            
            var body = damageInfo.attacker.GetComponent<CharacterBody>();
            if (!body)
                goto end;

            if (!body.inventory)
                goto end;

            var count = body.inventory.GetItemCount(Item.itemIndex);
            if (count > 0 && Util.CheckRoll(scaler.GetValue(count) * damageInfo.procCoefficient, body.master))
            {
                //var fireworkPos = victim.transform;
                var victimBody = victim.GetComponent<CharacterBody>();

                // Try to refine fireworkPos using a raycast
                var basePos = damageInfo.position;
                if (victimBody && Vector3.Distance(basePos, Vector3.zero) < Mathf.Epsilon)
                {
                    basePos = victimBody.mainHurtBox.randomVolumePoint;
                }

                var bestPoint = basePos;
                var bestHeight = basePos.y;
                
                var hits = Physics.RaycastAll(basePos, Vector3.up, MAX_FIREWORK_HEIGHT);
                foreach (var hit in hits)
                {
                    var cm = hit.transform.GetComponentInParent<CharacterModel>();
                    if (!cm)
                        continue;
                    
                    var cb = cm.body;
                    if (!cb)
                        continue;

                    if (cb != victimBody)
                        continue;
                    
                    var hurtbox = hit.transform.GetComponentInChildren<HurtBox>();
                    if (hurtbox)
                    {
                        var col = hurtbox.collider;
                        if (!col)
                            continue;
                        
                        var highestPoint = col.ClosestPoint(basePos + MAX_FIREWORK_HEIGHT * Vector3.up);
                        if (highestPoint.y > bestHeight)
                        {
                            bestPoint = highestPoint;
                            bestHeight = highestPoint.y;
                        }
                    }
                }
                
                
                ExtraFireworks.CreateLauncher(body, bestPoint + Vector3.up * 2f, numFireworks.Value);
                damageInfo.procChainMask.AddProc(ProcType.MicroMissile);
            }

            end:
            orig(self, damageInfo, victim);
        };
    }
}