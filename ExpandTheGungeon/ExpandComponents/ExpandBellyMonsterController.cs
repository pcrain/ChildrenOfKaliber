﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Dungeonator;
using System.Collections;
using ExpandTheGungeon.ExpandUtilities;
using ExpandTheGungeon.ItemAPI;

namespace ExpandTheGungeon.ExpandComponents {

    public class ExpandBellyMonsterController : DungeonPlaceableBehaviour, IPlaceConfigurable {

        public ExpandBellyMonsterController() {
            Awakened = false;
            PlayerEaten = false;

            SlamScreenShakeSettings = new ScreenShakeSettings() {
                magnitude = 0.25f,
                speed = 10f,
                time = 0.15f,
                falloff = 0.5f,
                direction = new Vector2(0, -1),
                vibrationType = ScreenShakeSettings.VibrationType.Auto,
                simpleVibrationTime = Vibration.Time.Normal,
                simpleVibrationStrength = Vibration.Strength.Medium

            };

        }

        public RoomHandler m_ParentRoom;
        public GameObject[] ImpactVFXObjects;

        public ScreenShakeSettings SlamScreenShakeSettings;
        
        public bool Awakened;

        
        private bool m_Triggered;
        private bool PlayerEaten;

        private GameObject m_EntranceTriggerObject;

        private void Start() {
            
            sprite.HeightOffGround = -7;
            sprite.UpdateZDepth();
        }

        private void Update() {
            if (!Awakened | PlayerEaten) { return; }
            if (!m_Triggered) {
                m_Triggered = true;
                m_ParentRoom.CompletelyPreventLeaving = true;
                m_ParentRoom.npcSealState = RoomHandler.NPCSealState.SealAll;
                m_ParentRoom.SealRoom();
                // Minimap.Instance.PreventAllTeleports = true;
                GameObject m_BackSpriteObject = new GameObject("Worm BackSprite");
                ItemBuilder.AddSpriteToObject(m_BackSpriteObject, "ExpandTheGungeon/Textures/BellyAssets/BellyMonster/Belly_Monster_BackEnd", false, false);
                m_BackSpriteObject.transform.position = transform.position;
                m_BackSpriteObject.transform.parent = transform;

                GameManager.Instance.MainCameraController.SetManualControl(true, true);
                spriteAnimator.AnimationEventTriggered = (Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>)Delegate.Combine(spriteAnimator.AnimationEventTriggered, new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>(AnimationEventTriggered));
                spriteAnimator.Play("MonsterChase");
                specRigidbody.OnPreRigidbodyCollision += OnPreRigidBodyCollision;
            }

            if (specRigidbody.Velocity.x <= 0) { specRigidbody.Velocity = new Vector2(-1.5f, 0); }

            if (!specRigidbody.CanPush) { specRigidbody.CanPush = true; }

            if (m_Triggered) { GameManager.Instance.MainCameraController.OverridePosition = transform.position - new Vector3(-4, 0) + new Vector3(0, 8); }
        }


        private void AnimationEventTriggered(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame) {
            if (clip.GetFrame(frame).eventInfo == "slam") {

                GameManager.Instance.MainCameraController.DoScreenShake(SlamScreenShakeSettings, (transform.position + new Vector3(0, 2f)), false);

                if (ImpactVFXObjects != null && ImpactVFXObjects.Length > 0) {

                    float SpawnPositionX = UnityEngine.Random.Range((transform.position.x + 2), (transform.position.x + 3));
                    float SpawnPositionY = UnityEngine.Random.Range((transform.position.y + 2), (transform.position.y + 9));
                    float SpawnPositionX2 = UnityEngine.Random.Range((transform.position.x + 2), (transform.position.x + 3));
                    float SpawnPositionY2 = UnityEngine.Random.Range((transform.position.y + 2), (transform.position.y + 9));

                    Vector3 SpawnPosition = new Vector3(SpawnPositionX, SpawnPositionY);
                    Vector3 SpawnPosition2 = new Vector3(SpawnPositionX2, SpawnPositionY2);

                    Instantiate(BraveUtility.RandomElement(ImpactVFXObjects), SpawnPosition, Quaternion.identity);

                    if (BraveUtility.RandomBool()) {
                        Instantiate(BraveUtility.RandomElement(ImpactVFXObjects), SpawnPosition2, Quaternion.identity);
                    }

                    List<string> SoundFXList = new List<string>() { "Play_OBJ_rock_break_01", "Play_OBJ_boulder_crash_01", "Play_OBJ_stone_crumble_01" };
                    SoundFXList = SoundFXList.Shuffle();
                    AkSoundEngine.PostEvent(BraveUtility.RandomElement(SoundFXList), gameObject);
                    if (BraveUtility.RandomBool()) { AkSoundEngine.PostEvent("Play_PET_junk_splat_01", gameObject); }
                }
            }
        }
        
        public void OnPreRigidBodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider) {
            if (otherRigidbody.GetComponent<AIActor>() && otherRigidbody.GetComponent<AIActor>().healthHaver.IsAlive) {
                AIActor TargetAIActor = otherRigidbody.GetComponent<AIActor>();
                TargetAIActor.healthHaver.ApplyDamage(500, new Vector2(1, 0), "Big Ass Worm", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, true);
                return;
            } else if (!PlayerEaten && otherRigidbody.GetComponent<PlayerController>()) {
                PlayerEaten = true;
                otherRigidbody.GetComponent<PlayerController>().SetInputOverride("got eaten");
                specRigidbody.Velocity = Vector2.zero;
                spriteAnimator.Stop();
                StartCoroutine(HandleTransitionToBellyFloor(otherRigidbody.GetComponent<PlayerController>()));
                return;
            } else if (otherRigidbody.GetComponent<MajorBreakable>()) {
                Vector3 ObjectPosition = otherRigidbody.GetUnitCenter(ColliderType.Ground);
                otherRigidbody.GetComponent<MajorBreakable>().Break(new Vector2(1, 0));
                Exploder.DoDefaultExplosion(ObjectPosition, new Vector2(1, 0), null, true, CoreDamageTypes.None, true);
            } else if (otherRigidbody.GetComponent<MinorBreakable>()) {
                otherRigidbody.GetComponent<MinorBreakable>().Break(new Vector2(1, 0));
            }
        }

        private IEnumerator HandleTransitionToBellyFloor(PlayerController player) {
            player.ToggleRenderer(false, "got eaten");
            player.ToggleGunRenderers(false, "got eaten");
            player.ToggleHandRenderers(false, "got eaten");
            yield return null;
            float elapsed = 0f;
            float duration = 0.5f;
            Vector3 startPos = player.specRigidbody.GetUnitCenter(ColliderType.Ground);
            Vector3 finalOffset = (transform.position + new Vector3(3, 6));
            GameObject dummySpriteObject = new GameObject("PlayerSpriteDupe", new Type[] { typeof(tk2dSprite) }) { layer = 22 };
            dummySpriteObject.transform.position = startPos;
            tk2dSprite targetSprite = dummySpriteObject.GetComponent<tk2dSprite>();
            ExpandUtility.DuplicateSprite(targetSprite, (player.sprite as tk2dSprite));
            targetSprite.SetSprite(player.sprite.spriteId);
            yield return null;
            while (elapsed < duration) {
                elapsed += BraveTime.DeltaTime;
                if (!targetSprite || !targetSprite.transform) { break; }
                targetSprite.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.01f, 0.01f, 1f), elapsed / duration);
                targetSprite.transform.position = Vector3.Lerp(startPos, finalOffset, elapsed / duration);
                yield return null;
            }
            // AkSoundEngine.PostEvent("Play_CHR_muncher_eat_01", gameObject);
            // yield return new WaitForSeconds(0.15f);
            AkSoundEngine.PostEvent("Play_VO_lichA_cackle_01", gameObject);
            Vector2 BottomOffset = dummySpriteObject.transform.position;
            Vector2 TopOffset = dummySpriteObject.transform.position + new Vector3(1, 1);
            Color TargetColor = new Color(0.5f, 0.1f, 0.1f);
            GlobalSparksDoer.DoRandomParticleBurst(25, BottomOffset, TopOffset, new Vector3(-1, 1), 70f, 0.5f, null, new float?(0.75f), new Color?(TargetColor), GlobalSparksDoer.SparksType.BLOODY_BLOOD);
            GlobalSparksDoer.DoRandomParticleBurst(25, BottomOffset, TopOffset, Vector3.left, 70f, 0.5f, null, new float?(1.5f), new Color?(TargetColor), GlobalSparksDoer.SparksType.BLOODY_BLOOD);
            GlobalSparksDoer.DoRandomParticleBurst(25, BottomOffset, TopOffset, Vector3.left, 70f, 0.5f, null, new float?(2.25f), new Color?(TargetColor), GlobalSparksDoer.SparksType.BLOODY_BLOOD);
            GlobalSparksDoer.DoRandomParticleBurst(25, BottomOffset, TopOffset, new Vector3(-1, -1), 70f, 0.5f, null, new float?(3), new Color?(TargetColor), GlobalSparksDoer.SparksType.BLOODY_BLOOD);
            yield return new WaitForSeconds(1);
            Pixelator.Instance.FadeToBlack(0.15f, false, 0f);
            yield return new WaitForSeconds(0.3f);
            AkSoundEngine.PostEvent("Play_CHR_muncher_chew_01", gameObject);
            yield return new WaitForSeconds(4);;
            Destroy(dummySpriteObject);
            player.ToggleRenderer(true, "got eaten");
            player.ToggleGunRenderers(true, "got eaten");
            player.ToggleHandRenderers(true, "got eaten");
            player.ClearAllInputOverrides();
            GameManager.Instance.LoadCustomLevel("tt_belly");            
            yield break;
        }

        public void ConfigureOnPlacement(RoomHandler room) {
            m_ParentRoom = room;

            m_EntranceTriggerObject = new GameObject("BellyMonsterAwakenTrigger") { layer = 0 };
            m_EntranceTriggerObject.transform.position = transform.position - new Vector3(14, 0);
            // m_EntranceTriggerObject.transform.parent = m_ParentRoom.hierarchyParent;
            ExpandUtility.GenerateOrAddToRigidBody(m_EntranceTriggerObject, CollisionLayer.Trap, PixelCollider.PixelColliderGeneration.Manual, IsTrigger: true, UsesPixelsAsUnitSize: true, offset: new IntVector2(0, -2), dimensions: new IntVector2(256, (m_ParentRoom.area.dimensions.y + 4 * 16)));

            ExpandBellyMonsterTriggerHandler EntranceTrigger = m_EntranceTriggerObject.AddComponent<ExpandBellyMonsterTriggerHandler>();
            EntranceTrigger.ParentRoom = m_ParentRoom;
            EntranceTrigger.MonsterController = this;

            if (EntranceTrigger.specRigidbody) {
                EntranceTrigger.specRigidbody.OnTriggerCollision = (SpeculativeRigidbody.OnTriggerDelegate)Delegate.Combine(EntranceTrigger.specRigidbody.OnTriggerCollision, new SpeculativeRigidbody.OnTriggerDelegate(EntranceTrigger.HandleTriggerCollision));
            }
        }

        protected override void OnDestroy() {
            specRigidbody.OnPreRigidbodyCollision -= OnPreRigidBodyCollision;
            base.OnDestroy();
        }

    }

    public class ExpandBellyMonsterTriggerHandler : BraveBehaviour {

        public ExpandBellyMonsterTriggerHandler() { m_Triggered = false; }
        
        public ExpandBellyMonsterController MonsterController;
        public RoomHandler ParentRoom;

        private bool m_Triggered;

        private void Start() { }

        private void Update() { }

        public void HandleTriggerCollision(SpeculativeRigidbody specRigidbody, SpeculativeRigidbody sourceSpecRigidbody, CollisionData collisionData) {
            if (m_Triggered) { return; }
            PlayerController player = specRigidbody.GetComponent<PlayerController>();
            if (player && MonsterController) {
                m_Triggered = true;
                MonsterController.Awakened = true;
                GameObject BellySoundFXObject = new GameObject("BellySoundFXObject") { layer = 0 };
                BellySoundFXObject.transform.position = (transform.position - new Vector3(15, 0) + new Vector3(0, 8));
                BellySoundFXObject.transform.parent = ParentRoom.hierarchyParent;
                AkSoundEngine.PostEvent("Play_OBJ_moondoor_close_01", BellySoundFXObject);
                Destroy(gameObject);
                return;
            }
        }
        
        protected override void OnDestroy() { base.OnDestroy(); }

    }
}
