﻿using HarmonyLib;
using UnityEngine;

namespace No_Player_Healing
{
    [HarmonyPatch(typeof(PLPlayer), "Update")]
    internal class Player
    {
        public static float currentPlayerHealth = float.MaxValue;
        static bool wasDead = false;
        private static int survivalBonusCounter = 0;
        private static int healthBoost = 0;
        private static float lastTimeAlived = float.MaxValue;

        static void Postfix(PLPlayer __instance)
        {
            if (__instance == PLNetworkManager.Instance.LocalPlayer)
            {
                if (__instance.GetClassID() < 0) return; //fixes ArgumentOutOfRangeException before player selects class
                PLPawn pawn = __instance.GetPawn();
                if (pawn == null) //Pawn is deleted when player is dead, then a new pawn is created when player respawns
                {
                    wasDead = true;
                    return;
                }

                //Allow health reset when player respawns or is revived
                if (wasDead && !pawn.IsDead)
                {
                    lastTimeAlived = Time.time;
                    currentPlayerHealth = float.MaxValue;
                }
                wasDead = pawn.IsDead;

                //Increase health when max health is increased by warping.
                int counter = PLServer.Instance.ClassInfos[__instance.GetClassID()].SurvivalBonusCounter;
                if (counter < survivalBonusCounter)
                {
                    survivalBonusCounter = counter;
                }
                else if (counter > survivalBonusCounter)
                {
                    currentPlayerHealth += (counter - survivalBonusCounter) * 5;
                    if (pawn.Health > 0 && Mod.Instance.IsEnabled())
                    {
                        pawn.Health += (counter - survivalBonusCounter) * 5;
                    }
                    survivalBonusCounter = counter;
                }

                //Revert any other health increase
                if (Time.time - lastTimeAlived > 5)
                {
                    if (currentPlayerHealth > pawn.MaxHealth)
                    {
                        currentPlayerHealth = pawn.MaxHealth;
                    }

                    if (pawn.Health < currentPlayerHealth && pawn.Health > 0 && !pawn.IsDead)
                    {
                        currentPlayerHealth = pawn.Health;
                    }
                    else if (pawn.Health > currentPlayerHealth && Mod.Instance.IsEnabled())
                    {
                        pawn.Health = currentPlayerHealth;
                    }
                }

                //Increase health when max health is increased by health boost talent points.
                int boost = __instance.Talents[0] + __instance.Talents[57];
                if (boost < healthBoost)
                {
                    currentPlayerHealth -= (healthBoost - boost) * 20;
                    if (currentPlayerHealth <= 0)
                    {
                        currentPlayerHealth = 0.4f;
                    }
                    healthBoost = boost;
                }
                else if (boost > healthBoost)
                {
                    currentPlayerHealth += (boost - healthBoost) * 20;
                    if (pawn.Health > 0 && Mod.Instance.IsEnabled())
                    {
                        pawn.Health += (boost - healthBoost) * 20;
                    }
                    healthBoost = boost;
                }
            }
        }
    }
}
