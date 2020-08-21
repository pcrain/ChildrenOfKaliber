﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;
using Gungeon;
using ItemAPI;
using MonoMod;
using MonoMod.RuntimeDetour;
using Random = UnityEngine.Random;
using UnityEngine;
using FullInspector;

namespace Items
{
    class MalachiteAmmolet : PassiveItem
    {
        public static void Init()
        {

            string itemName = "Malachite Ammolet";
            string resourceName = "Items/Resources/malachite_ammolet.png";

            GameObject obj = new GameObject(itemName);

            var item = obj.AddComponent<MalachiteAmmolet>();

            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Envenomed";
            string longDesc = "Creates a pool of poison upon using a blank. Gives poison immunity.\n\nThis ammolet was found in the corpse of an extremely venomous snake.";
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1, StatModifier.ModifyMethod.ADDITIVE);


            ItemBuilder.SetupItem(item, shortDesc, longDesc, "cel");
            item.quality = PickupObject.ItemQuality.B;
            item.sprite.IsPerpendicular = true;


        }

        private void FireGoopBlank(PlayerController player, int blanks)
        {
            var bundle = ResourceManager.LoadAssetBundle("shared_auto_001");
            var goop = bundle.LoadAsset<GoopDefinition>("assets/data/goops/poison goop.asset");
            var ddgm = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(goop);
            ddgm.TimedAddGoopCircle(Owner.sprite.WorldCenter, 8f, .35f);
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            this.m_poisImmunity = new DamageTypeModifier();
            this.m_poisImmunity.damageMultiplier = 0f;
            this.m_poisImmunity.damageType = CoreDamageTypes.Poison;
            player.healthHaver.damageTypeModifiers.Add(this.m_poisImmunity);
            player.OnUsedBlank += this.FireGoopBlank;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnUsedBlank -= this.FireGoopBlank;
            player.healthHaver.damageTypeModifiers.Remove(this.m_poisImmunity);
            debrisObject.GetComponent<MalachiteAmmolet>().m_pickedUpThisRun = true;
            return debrisObject;
        }
        private DamageTypeModifier m_poisImmunity;
    }
}
