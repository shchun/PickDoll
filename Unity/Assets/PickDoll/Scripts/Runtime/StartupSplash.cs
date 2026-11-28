using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace PickDoll
{
    public sealed class StartupSplash : MonoBehaviour
    {
        public Texture2D mascot;
        IEnumerator Start()
        {
            var root = new GameObject("PickDoll welcome", typeof(Canvas), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(900,1400); scaler.matchWidthOrHeight = .5f;
            // Visual only: never blocks controls or leaves an input-blocking overlay behind.
            var group = root.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            var background = new GameObject("Cream", typeof(RectTransform), typeof(Image)); background.transform.SetParent(root.transform,false);
            var rect = background.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(1,.973f,.93f); background.GetComponent<Image>().raycastTarget = false;
            var face = new GameObject("Bunny",typeof(RectTransform),typeof(RawImage)); face.transform.SetParent(root.transform,false);
            var fr = face.GetComponent<RectTransform>(); fr.sizeDelta = new Vector2(440,440); fr.anchoredPosition = new Vector2(0,90);
            face.GetComponent<RawImage>().texture = mascot; face.GetComponent<RawImage>().raycastTarget = false;
            var title = new GameObject("PickDoll",typeof(RectTransform),typeof(Text)); title.transform.SetParent(root.transform,false);
            var tr = title.GetComponent<RectTransform>(); tr.sizeDelta = new Vector2(700,100); tr.anchoredPosition = new Vector2(0,-175);
            var text = title.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = "PickDoll"; text.fontSize = 72; text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.42f,.20f,.32f); text.raycastTarget = false;
            // Once the first game frames are ready, transition immediately rather than hold a timer.
            yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
            float elapsed = 0;
            while (elapsed < .45f) { elapsed += Time.unscaledDeltaTime; group.alpha = 1 - Mathf.SmoothStep(0,1,elapsed/.45f); yield return null; }
            Destroy(root); Destroy(this);
        }
    }
}
