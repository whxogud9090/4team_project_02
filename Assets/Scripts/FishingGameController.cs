using System.Collections;
using UnityEngine;

/// <summary>Playable fishing prototype: move, face the water, cast, wait for a bite, then keep the fish inside the reel zone.</summary>
[ExecuteAlways]
public sealed class FishingGameController : MonoBehaviour
{
    private enum State { Explore, Waiting, Reeling, Result }
    private State state;
    private PixelCharacter character;
    private Camera gameCamera;
    private GameObject bobber;
    private Texture2D[] fishTextures;
    private Texture2D currentFish;
    private float waitTimer;
    private float reelPosition = .4f;
    private float reelVelocity;
    private float fishPosition = .55f;
    private float fishVelocity;
    private float progress;
    private float escape;
    private string message = "WASD / Arrow keys to walk  •  Face the water and press F to fish";
    private string result = "";

    private void OnEnable()
    {
        if (!Application.isPlaying) BuildEditorPreview();
    }

    private void Awake()
    {
        if (!Application.isPlaying) return;
        var preview = transform.Find("Scene Preview");
        if (preview != null) Destroy(preview.gameObject);
        BuildWorld();
        fishTextures = new[] {
            Resources.Load<Texture2D>("Sprites/Fish/atlantic_salmon"),
            Resources.Load<Texture2D>("Sprites/Fish/common_carp"),
            Resources.Load<Texture2D>("Sprites/Fish/northern_pike")
        };
    }

    private void BuildWorld()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 4.5f;
        gameCamera.transform.position = new Vector3(0, 0, -10);
        gameCamera.backgroundColor = new Color(.16f, .52f, .68f);

        CreateRect("Grass Shore", new Vector2(-3.8f, 0), new Vector2(4.4f, 9), new Color(.31f, .67f, .27f), -3);
        CreateRect("Water", new Vector2(2.2f, 0), new Vector2(7.2f, 9), new Color(.12f, .50f, .69f), -4);
        for (var y = -4f; y <= 4f; y += .65f)
            CreateRect("Water Ripple", new Vector2(2.2f + Mathf.Sin(y * 8f) * .4f, y), new Vector2(4.8f, .025f), new Color(.42f, .80f, .86f, .55f), -2);
        CreateRect("Dock", new Vector2(-.1f, -1.1f), new Vector2(2.5f, 1.0f), new Color(.43f, .25f, .12f), -1);
        CreateRect("Fishing Zone", new Vector2(1.35f, -1.1f), new Vector2(.16f, 1.15f), new Color(1f, .86f, .36f, .7f), 0);

        var player = new GameObject("Player (Pixel Crawler)");
        player.transform.position = new Vector3(-.75f, -1.1f, 0);
        character = player.AddComponent<PixelCharacter>();
        character.Setup();
    }

    // A lightweight real Scene-view preview; the complete interactive objects are made when Play is pressed.
    private void BuildEditorPreview()
    {
        if (transform.Find("Scene Preview") != null) return;
        var root = new GameObject("Scene Preview").transform;
        root.SetParent(transform);
        CreatePreviewRect(root, "Grass Shore", new Vector2(-3.8f, 0), new Vector2(4.4f, 9), new Color(.31f, .67f, .27f), -3);
        CreatePreviewRect(root, "Water", new Vector2(2.2f, 0), new Vector2(7.2f, 9), new Color(.12f, .50f, .69f), -4);
        CreatePreviewRect(root, "Dock", new Vector2(-.1f, -1.1f), new Vector2(2.5f, 1.0f), new Color(.43f, .25f, .12f), -1);
        CreatePreviewRect(root, "Fishing Zone", new Vector2(1.35f, -1.1f), new Vector2(.16f, 1.15f), new Color(1f, .86f, .36f, .7f), 0);
    }

    private static void CreatePreviewRect(Transform parent, string objectName, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        var go = CreateRect(objectName, position, size, color, sortingOrder);
        go.transform.SetParent(parent, true);
    }

    private static GameObject CreateRect(string objectName, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        var go = new GameObject(objectName);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white); tex.Apply();
        renderer.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        renderer.color = color; renderer.sortingOrder = sortingOrder;
        go.transform.localScale = size;
        return go;
    }

    private void Update()
    {
        if (state == State.Explore)
        {
            character.Move();
            if (Vector2.Distance(character.transform.position, new Vector2(-.75f, -1.1f)) < 1.5f && Input.GetKeyDown(KeyCode.F)) Cast();
        }
        else if (state == State.Waiting)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) ResetFishing("Fishing cancelled.");
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0) Bite();
        }
        else if (state == State.Reeling) Reel();
        else if (state == State.Result && Input.GetKeyDown(KeyCode.F)) ResetFishing("Face the water and press F to fish");
    }

    private void Cast()
    {
        state = State.Waiting; character.SetFishing(true);
        message = "Casting… wait for a bite  (ESC to cancel)";
        bobber = CreateRect("Bobber", new Vector2(2.1f, -1.1f), new Vector2(.14f, .14f), new Color(1f, .28f, .2f), 2);
        waitTimer = Random.Range(2f, 6f);
    }

    private void Bite()
    {
        state = State.Reeling; currentFish = fishTextures[Random.Range(0, fishTextures.Length)];
        fishPosition = Random.Range(.15f, .85f); reelPosition = .4f; reelVelocity = 0; fishVelocity = Random.Range(-.15f, .15f);
        progress = escape = 0; message = "BITE! Hold left mouse / SPACE to pull the white zone right. Keep fish inside it!";
        if (bobber != null) bobber.GetComponent<SpriteRenderer>().color = Color.yellow;
    }

    private void Reel()
    {
        float input = (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) ? 1f : -1f;
        reelVelocity += input * 1.35f * Time.deltaTime;
        reelVelocity *= Mathf.Exp(-4f * Time.deltaTime);
        reelPosition = Mathf.Clamp01(reelPosition + reelVelocity * Time.deltaTime);
        fishVelocity += Mathf.Sin(Time.time * 3.1f) * .22f * Time.deltaTime;
        fishVelocity = Mathf.Clamp(fishVelocity, -.38f, .38f);
        fishPosition += fishVelocity * Time.deltaTime;
        if (fishPosition < .02f || fishPosition > .98f) fishVelocity *= -1f;
        fishPosition = Mathf.Clamp(fishPosition, .02f, .98f);
        float zoneSize = currentFish == fishTextures[2] ? .15f : currentFish == fishTextures[1] ? .20f : .26f;
        bool aligned = fishPosition >= reelPosition && fishPosition <= reelPosition + zoneSize;
        progress = Mathf.Clamp01(progress + (aligned ? .27f : -.12f) * Time.deltaTime);
        escape = Mathf.Clamp01(escape + (aligned ? -.14f : .18f) * Time.deltaTime);
        if (progress >= 1) ResetFishing("CAUGHT! Press F to cast again.");
        else if (escape >= 1) ResetFishing("The fish escaped. Press F to try again.");
        else if (Input.GetKeyDown(KeyCode.Escape)) ResetFishing("Fishing cancelled.");
    }

    private void ResetFishing(string newMessage)
    {
        if (state == State.Reeling && progress >= 1) result = currentFish == fishTextures[2] ? "Northern Pike" : currentFish == fishTextures[1] ? "Common Carp" : "Atlantic Salmon";
        state = State.Result; message = newMessage; character.SetFishing(false);
        if (bobber != null) Destroy(bobber);
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, normal = { textColor = Color.white } };
        GUI.Box(new Rect(Screen.width / 2 - 310, 20, 620, 38), message); GUI.Label(new Rect(Screen.width / 2 - 300, 25, 600, 28), message, style);
        if (state != State.Reeling) { if (!string.IsNullOrEmpty(result)) GUI.Label(new Rect(Screen.width/2-180, 70, 360, 35), "Caught: " + result, style); return; }
        var track = new Rect(Screen.width / 2 - 350, Screen.height - 150, 700, 38);
        GUI.Box(track, "");
        float zoneSize = currentFish == fishTextures[2] ? .15f : currentFish == fishTextures[1] ? .20f : .26f;
        GUI.color = Color.white; GUI.Box(new Rect(track.x + reelPosition * track.width, track.y + 3, zoneSize * track.width, track.height - 6), "");
        GUI.color = Color.white;
        if (currentFish != null) GUI.DrawTexture(new Rect(track.x + fishPosition * track.width - 16, track.y - 10, 32, 32), currentFish, ScaleMode.ScaleToFit, true);
        GUI.Box(new Rect(track.x, track.y + 55, track.width * progress, 16), ""); GUI.Label(new Rect(track.x, track.y + 75, 200, 24), "CATCH " + Mathf.RoundToInt(progress * 100) + "%");
        GUI.Box(new Rect(track.x + track.width - track.width * escape, track.y + 55, track.width * escape, 16), ""); GUI.Label(new Rect(track.x + 500, track.y + 75, 200, 24), "ESCAPE " + Mathf.RoundToInt(escape * 100) + "%", style);
    }
}
