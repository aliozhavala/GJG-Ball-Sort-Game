using System;
using System.Collections.Generic;
using System.IO;
using BallSort.Core.Model;
using BallSort.Game.Views;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BallSort.Editor.Art
{
    /// <summary>
    /// Renders the tutorial popups' pictures (docs/screens.md, Tutorials) from the board's own views and
    /// art: a hidden ball, a large tube on its stand, a veiled tube, an iced tube, a lock box and its key.
    /// </summary>
    /// <remarks>
    /// The views need their <c>Awake</c>, so this runs in Play mode in the Level scene. Each picture is
    /// built from the prefabs and configs the board uses, far from the board, rendered by its own camera
    /// to a transparent PNG in <see cref="Folder"/>, and destroyed. Afterwards (out of Play mode) run
    /// Prepare Sprites to trim and import them; <c>TutorialConfig</c> points at them.
    /// </remarks>
    internal static class TutorialPictureBuilder
    {
        public const string Folder = SpriteRules.Root + "/UI/Tutorials";

        private const string TubePrefab = "Assets/Prefabs/Tube.prefab";
        private const string BallPrefab = "Assets/Prefabs/Ball.prefab";
        private const string CoverPrefab = "Assets/Prefabs/Cover.prefab";

        // Far from the board, so the picture's camera sees nothing else.
        private static readonly Vector3 Stage = new Vector3(1000f, 0f, 0f);
        private const float PixelsPerUnit = 220f;
        private const float Padding = 0.12f;

        private sealed class Context
        {
            public TubeVisualConfig Tubes;
            public AnimationConfig Animation;
            public ColorPalette Palette;
            public LockPalette Locks;
            public TubeView TubePrefab;
            public BallView BallPrefab;
            public CoverView CoverPrefab;
            public Transform Root;
        }

        [MenuItem("Ball Sort/Art/Build Tutorial Pictures")]
        private static void Build()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Build Tutorial Pictures",
                    "Enter Play mode in the Level scene first: the board's views only work once they are awake.", "OK");
                return;
            }

            var context = new Context
            {
                Tubes = LoadFirst<TubeVisualConfig>(),
                Animation = LoadFirst<AnimationConfig>(),
                Palette = LoadFirst<ColorPalette>(),
                Locks = LoadFirst<LockPalette>(),
                TubePrefab = AssetDatabase.LoadAssetAtPath<TubeView>(TubePrefab),
                BallPrefab = AssetDatabase.LoadAssetAtPath<BallView>(BallPrefab),
                CoverPrefab = AssetDatabase.LoadAssetAtPath<CoverView>(CoverPrefab),
            };

            Directory.CreateDirectory(Folder);
            Render(context, "Tutorial_Hidden", HiddenBall);
            Render(context, "Tutorial_BigTube", BigTube);
            Render(context, "Tutorial_Veil", Veil);
            Render(context, "Tutorial_Ice", Ice);
            Render(context, "Tutorial_LockBox", LockBox);
            Debug.Log($"Tutorial pictures written to {Folder}. Leave Play mode and run Prepare Sprites.");
        }

        // A tube whose top monster is shown and the two under it still hidden.
        private static void HiddenBall(Context c)
        {
            Tube(c, 0f, 4, false, new Ball(1, true), new Ball(5, true), new Ball(2));
        }

        // A large clay tube on its stand, filling with its colour (capacity 7: a 10 would be too tall for the popup).
        private static void BigTube(Context c)
        {
            TubeView tube = Tube(c, 0f, 7, true, new Ball(0), new Ball(0), new Ball(0), new Ball(0), new Ball(0), new Ball(0));
            tube.SetStand(c.Palette.GetStand(0));
        }

        // A tube under the towel, the badge showing the colour that lifts it.
        private static void Veil(Context c)
        {
            TubeView tube = Tube(c, 0f, 4, false, new Ball(1), new Ball(5), new Ball(6));
            CoverView veil = Instantiate(c.CoverPrefab, c.Root);
            veil.SetupVeil(3, c.Tubes, c.Palette, c.Animation);
            Cover(c, veil, tube);
        }

        // A capacity-4 clay tube in its ice, two of one colour inside.
        private static void Ice(Context c)
        {
            Tube(c, 0f, 4, true, new Ball(6), new Ball(6));
        }

        // A shut lock box, and next to it the monster carrying its key on top of its tube.
        private static void LockBox(Context c)
        {
            float pitch = c.Tubes.TubeWidth + c.Tubes.HorizontalSpacing;
            TubeView boxed = Tube(c, 0f, 4, false, new Ball(1), new Ball(4), new Ball(3), new Ball(7));
            Tube(c, pitch, 4, false, new Ball(5), new Ball(2, false, 0));
            CoverView box = Instantiate(c.CoverPrefab, c.Root);
            box.SetupLock(0, c.Tubes, c.Locks);
            Cover(c, box, boxed);
        }

        private static TubeView Tube(Context c, float x, int capacity, bool clay, params Ball[] balls)
        {
            TubeView tube = Instantiate(c.TubePrefab, c.Root);
            tube.Setup(0, capacity, clay, c.Tubes, c.Animation);
            tube.Place(new Vector3(x, 0f, 0f));
            foreach (Ball ball in balls)
            {
                BallView view = Instantiate(c.BallPrefab, c.Root);
                view.Initialize(c.Animation, null, null);
                view.Show(ball, c.Palette, c.Locks, c.Tubes.BallSize);
                tube.AddBall(view);
            }

            return tube;
        }

        private static void Cover(Context c, CoverView cover, TubeView tube)
        {
            cover.AddTube(tube);
            cover.FitToTubes(c.Tubes);
            cover.SetOpen(false);
        }

        private static T Instantiate<T>(T prefab, Transform parent) where T : Component =>
            Object.Instantiate(prefab, parent);

        private static void Render(Context context, string name, Action<Context> build)
        {
            var root = new GameObject(name);
            root.transform.position = Stage;
            context.Root = root.transform;
            try
            {
                build(context);
                Bounds bounds = DrawnBounds(root);
                bounds.Expand(Padding * 2f);
                int width = Mathf.CeilToInt(bounds.size.x * PixelsPerUnit);
                int height = Mathf.CeilToInt(bounds.size.y * PixelsPerUnit);

                var cameraObject = new GameObject("TutorialPictureCamera");
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.y;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.enabled = false;

                var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                camera.aspect = (float)width / height;
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                var picture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                picture.Apply();
                RenderTexture.active = previous;
                camera.targetTexture = null;

                File.WriteAllBytes($"{Folder}/{name}.png", picture.EncodeToPNG());
                Object.Destroy(picture);
                target.Release();
                Object.Destroy(target);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Bounds DrawnBounds(GameObject root)
        {
            var renderers = new List<Renderer>();
            root.GetComponentsInChildren(renderers);
            bool any = false;
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is TrailRenderer ||
                    renderer is SpriteRenderer { sprite: null })
                {
                    continue;
                }

                if (any)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    any = true;
                }
            }

            return bounds;
        }

        private static T LoadFirst<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}
