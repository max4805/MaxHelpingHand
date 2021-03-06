﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Linq;

namespace Celeste.Mod.MaxHelpingHand.Entities {
    [CustomEntity("MaxHelpingHand/ReskinnableCrushBlock")]
    public class ReskinnableCrushBlock : CrushBlock {
        public static void Load() {
            On.Celeste.CrushBlock.ctor_EntityData_Vector2 += onCrushBlockConstruct;
            IL.Celeste.CrushBlock.ctor_Vector2_float_float_Axes_bool += modCrushBlockSprites;
            IL.Celeste.CrushBlock.AddImage += modCrushBlockSprites;
            On.Celeste.CrushBlock.ActivateParticles += activateParticles;
        }

        public static void Unload() {
            On.Celeste.CrushBlock.ctor_EntityData_Vector2 -= onCrushBlockConstruct;
            IL.Celeste.CrushBlock.ctor_Vector2_float_float_Axes_bool -= modCrushBlockSprites;
            IL.Celeste.CrushBlock.AddImage -= modCrushBlockSprites;
            On.Celeste.CrushBlock.ActivateParticles -= activateParticles;
        }

        private static void onCrushBlockConstruct(On.Celeste.CrushBlock.orig_ctor_EntityData_Vector2 orig, CrushBlock self, EntityData data, Vector2 offset) {
            // we are using a hook rather than the constructor, because we want to run our code before the base constructor.
            if (self is ReskinnableCrushBlock crushBlock) {
                crushBlock.spriteDirectory = data.Attr("spriteDirectory", "objects/crushblock");
            }

            orig(self, data, offset);
        }

        private static void modCrushBlockSprites(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // look where the base path to the blocks is used, and replace it for custom crush blocks.
            string[] stringsToLookUp = { "objects/crushblock/block", "objects/crushblock/lit_left", "objects/crushblock/lit_right", "objects/crushblock/lit_top", "objects/crushblock/lit_bottom" };
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldstr && stringsToLookUp.Contains((string) instr.Operand))) {
                Logger.Log("MaxHelpingHand/ReskinnableCrushBlock", $"Injecting code to reskin Kevins at {cursor.Index} in IL for {cursor.Method.Name}");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, CrushBlock, string>>((orig, self) => {
                    if (self is ReskinnableCrushBlock crushBlock) {
                        return orig.Replace("objects/crushblock", crushBlock.spriteDirectory);
                    }
                    return orig;
                });
            }
        }


        private string spriteDirectory;

        private ParticleType crushParticleColor;
        private ParticleType activateParticleColor;

        public ReskinnableCrushBlock(EntityData data, Vector2 offset) : base(data, offset) {
            DynData<CrushBlock> self = new DynData<CrushBlock>(this);
            bool giant = self.Get<bool>("giant");
            Sprite face = self.Get<Sprite>("face");

            // rebuild the face in code with the sprites in our custom directory.
            face.Reset(GFX.Game, spriteDirectory + "/");
            if (giant) {
                /*
                  <Loop id="idle" path="giant_block" frames="0" delay="0.08"/>
                  <Anim id="hurt"  path="giant_block" frames="8-12" delay="0.08" goto="idle"/>
                  <Anim id="hit" path="giant_block" frames="0-5" delay="0.08"/>
                  <Loop id="right" path="giant_block" frames="6,7"  delay="0.08"/>
                */
                face.AddLoop("idle", "giant_block", 0.08f, 0);
                face.Add("hurt", "giant_block", 0.08f, "idle", 8, 9, 10, 11, 12);
                face.Add("hit", "giant_block", 0.08f, 0, 1, 2, 3, 4, 5);
                face.AddLoop("right", "giant_block", 0.08f, 6, 7);
            } else {
                /*
                  <Loop id="idle" path="idle_face" delay="0.08"/>
                  <Anim id="hurt" path="hurt" frames="3-12" delay="0.08" goto="idle"/>
                  <Anim id="hit" path="hit" delay="0.08"/>
                  <Loop id="left" path="hit_left" delay="0.08"/>
                  <Loop id="right" path="hit_right" delay="0.08"/>
                  <Loop id="up" path="hit_up" delay="0.08"/>
                  <Loop id="down" path="hit_down" delay="0.08"/>
                */

                face.AddLoop("idle", "idle_face", 0.08f);
                face.Add("hurt", "hurt", 0.08f, "idle", 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
                face.Add("hit", "hit", 0.08f);
                face.AddLoop("left", "hit_left", 0.08f);
                face.AddLoop("right", "hit_right", 0.08f);
                face.AddLoop("up", "hit_up", 0.08f);
                face.AddLoop("down", "hit_down", 0.08f);
            }
            face.CenterOrigin();
            face.Play("idle");

            // customize the fill color.
            self["fill"] = Calc.HexToColor(data.Attr("fillColor", "62222b"));

            crushParticleColor = new ParticleType(P_Crushing) {
                Color = Calc.HexToColor(data.Attr("crushParticleColor1", "ff66e2")),
                Color2 = Calc.HexToColor(data.Attr("crushParticleColor2", "68fcff"))
            };
            activateParticleColor = new ParticleType(P_Activate) {
                Color = Calc.HexToColor(data.Attr("activateParticleColor1", "5fcde4")),
                Color2 = Calc.HexToColor(data.Attr("activateParticleColor2", "ffffff"))
            };
        }

        public override void Update() {
            ParticleType origCrushParticle = P_Crushing;
            P_Crushing = crushParticleColor;

            base.Update();

            P_Crushing = origCrushParticle;
        }

        private static void activateParticles(On.Celeste.CrushBlock.orig_ActivateParticles orig, CrushBlock self, Vector2 dir) {
            if (self is ReskinnableCrushBlock crush) {
                ParticleType origActivateParticle = P_Activate;
                P_Activate = crush.activateParticleColor;

                orig(self, dir);

                P_Activate = origActivateParticle;
            } else {
                orig(self, dir);
            }
        }
    }
}
