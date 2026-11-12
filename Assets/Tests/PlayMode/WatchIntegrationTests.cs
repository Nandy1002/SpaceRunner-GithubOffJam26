using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SpaceRunner.Core;
using SpaceRunner.Demo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SpaceRunner.Tests
{
    public sealed class WatchIntegrationTests
    {
        private WatchPrototypeBootstrap prototype;
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
#endif

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Batch tests have no focused Game View. Keep synthetic input routed to the game.
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            yield return SceneManager.LoadSceneAsync("WatchPrototype");
            yield return null;
            prototype = Object.FindFirstObjectByType<WatchPrototypeBootstrap>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            prototype.Session.StartRun();
            yield return new WaitForSecondsRealtime(0.12f);
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (prototype != null) prototype.Session.ReturnToMenu();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
#endif
        }

        [UnityTest]
        public IEnumerator ROpensAndClosesWhileTimeScaleIsZero()
        {
            yield return PressR();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(prototype.Watch.State.Phase, Is.EqualTo(WatchPhase.Open));
            Assert.That(prototype.WatchMenu.IsVisible, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(prototype.Session.CanControlPlayer, Is.False);
            var arm = prototype.System.Rig.Presentation.gameObject;
            foreach (var renderer in arm.GetComponentsInChildren<Renderer>())
                Assert.That(renderer.forceRenderingOff, Is.True, "The wrist and arm must disappear behind the expanded menu.");
            Assert.That(prototype.System.Rig.Focus.IsExpanded, Is.True);
            Assert.That(prototype.WatchMenu.transform.IsChildOf(arm.transform), Is.True, "The UI remains attached to the physical wrist.");
            var elapsed = prototype.Session.Timer.ElapsedSeconds;
            var beacon = GameObject.Find("Rotating Beacon").transform;
            var rotation = beacon.rotation;
            var motor = Object.FindFirstObjectByType<FirstPersonController>().transform;
            var position = motor.position;
            var viewRotation = Camera.main.transform.rotation;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift, Key.Space));
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(90, 60));
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(prototype.Player.GetComponent<DashAbility>().IsActive, Is.False, "Pause must also block ability inputs.");
            Assert.That(prototype.Session.Timer.ElapsedSeconds, Is.EqualTo(elapsed));
            Assert.That(Quaternion.Angle(beacon.rotation, rotation), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(motor.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(Camera.main.transform.rotation, viewRotation), Is.LessThan(0.01f));
            yield return PressR();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(prototype.Watch.State.Phase, Is.EqualTo(WatchPhase.Closed));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            foreach (var renderer in arm.GetComponentsInChildren<Renderer>())
                Assert.That(renderer.forceRenderingOff, Is.False, "Closing must restore the arm for the next animation.");
            foreach (var canvas in arm.GetComponentsInChildren<Canvas>())
                Assert.That(canvas.enabled, Is.True);
            Assert.That(prototype.Session.CanControlPlayer, Is.True);
        }

        [UnityTest]
        public IEnumerator DisabledPauseOptionKeepsWorldTimerMovementAndLookActive()
        {
            prototype.Watch.PauseGameplayWhileOpen = false;
            prototype.Watch.Toggle();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(prototype.WatchMenu.IsVisible, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(prototype.Session.CanControlPlayer, Is.True);
            Assert.That(prototype.Session.PointerRequests.IsPaused, Is.True);
            var elapsed = prototype.Session.Timer.ElapsedSeconds;
            var motor = Object.FindFirstObjectByType<FirstPersonController>().transform;
            var position = motor.position;
            var rotation = Camera.main.transform.rotation;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(90, 30));
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(prototype.Session.Timer.ElapsedSeconds, Is.GreaterThan(elapsed));
            Assert.That(Vector3.Distance(motor.position, position), Is.GreaterThan(0.01f));
            Assert.That(Quaternion.Angle(Camera.main.transform.rotation, rotation), Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator WorldSpaceMenuReceivesPointerRaycasts()
        {
            prototype.Watch.Toggle();
            yield return new WaitForSecondsRealtime(0.8f);
            var button = FindButton(prototype.WatchMenu.transform, "Resume");
            var canvas = prototype.WatchMenu.GetComponent<Canvas>();
            var screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, button.transform.position);
            Assert.That(screen.x, Is.InRange(0f, (float)Screen.width), "The clickable watch button must also be onscreen.");
            Assert.That(screen.y, Is.InRange(0f, (float)Screen.height), "The clickable watch button must also be onscreen.");
            var corners = new Vector3[4];
            canvas.GetComponent<RectTransform>().GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var viewportPoint = canvas.worldCamera.WorldToViewportPoint(corner);
                Assert.That(viewportPoint.z, Is.GreaterThan(canvas.worldCamera.nearClipPlane));
                Assert.That(viewportPoint.x, Is.InRange(0f, 1f), "Enlarged watch face must fit the camera horizontally.");
                Assert.That(viewportPoint.y, Is.InRange(0f, 1f), "Enlarged watch face must fit the camera vertically.");
            }
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, results);
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(button));
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EndRunConfirmationReturnsToMenuAndNextRunStartsFresh()
        {
            prototype.Watch.Toggle();
            yield return new WaitForSecondsRealtime(0.8f);
            FindButton(prototype.WatchMenu.transform, "End Run").onClick.Invoke();
            FindButton(prototype.WatchMenu.transform, "Cancel").onClick.Invoke();
            Assert.That(prototype.Session.IsPlaying, Is.True);
            FindButton(prototype.WatchMenu.transform, "End Run").onClick.Invoke();
            FindButton(prototype.WatchMenu.transform, "Confirm").onClick.Invoke();
            Assert.That(prototype.Session.IsPlaying, Is.False);
            Assert.That(prototype.WatchMenu.IsVisible, Is.False);
            Assert.That(prototype.Watch.State.Phase, Is.EqualTo(WatchPhase.Closed));
            Assert.That(Time.timeScale, Is.Zero);
            prototype.Session.StartRun();
            Assert.That(prototype.Session.Timer.ElapsedSeconds, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TabKeepsGravitySelectionAndRTemporarilyBlocksIt()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            yield return null; yield return null;
            Assert.That(prototype.Watch.State.Phase, Is.EqualTo(WatchPhase.Closed));
            Assert.That(prototype.System.GravityMenu.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab, Key.R));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(prototype.WatchMenu.IsVisible, Is.True);
            Assert.That(prototype.System.GravityMenu.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(InputManager.Instance.GravitySelectionBlocked, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return PressR();
            yield return new WaitForSecondsRealtime(0.8f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            yield return null; yield return null;
            Assert.That(prototype.System.GravityMenu.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator GravitySelectionStillWorksAndWatchFollowsAnInvertedPlayer()
        {
            var border = (RectTransform)prototype.System.GravityMenu.transform.Find("Selection Border");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = RectTransformUtility.WorldToScreenPoint(null, border.TransformPoint(new Vector3(0, 145, 0))) });
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(prototype.System.GravityMenu.SelectedGravityIndex, Is.EqualTo(4));
            Assert.That(Physics.gravity.y, Is.GreaterThan(9));
            yield return PressR();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(prototype.System.Rig.Focus.IsExpanded, Is.True);
            var button = FindButton(prototype.WatchMenu.transform, "Resume");
            var screen = prototype.System.Rig.Focus.Camera.WorldToViewportPoint(button.transform.position);
            Assert.That(screen.x, Is.InRange(0f, 1f)); Assert.That(screen.y, Is.InRange(0f, 1f));
            prototype.Session.ReturnToMenu(); prototype.Session.StartRun();
            Assert.That(Physics.gravity.y, Is.LessThan(-9));
        }

        [UnityTest]
        public IEnumerator DisabledControllerReleasesOnlyItsOwnPause()
        {
            using var externalPause = prototype.Session.Pause.Acquire();
            prototype.Watch.Toggle();
            yield return new WaitForSecondsRealtime(0.8f);
            prototype.Watch.enabled = false;
            Assert.That(prototype.WatchMenu.IsVisible, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            externalPause.Dispose();
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        private IEnumerator PressR()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static Button FindButton(Transform root, string name)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                if (button.name == name) return button;
            Assert.Fail("Missing button: " + name);
            return null;
        }
    }
}
