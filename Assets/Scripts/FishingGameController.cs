using System.Collections;
using UnityEngine;

/// <summary>Playable fishing prototype: move, face the water, cast, wait for a bite, then keep the fish inside the reel zone.</summary>
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
    private string message = "이동: WASD 또는 방향키";
    private string result = "";
    private bool initialized;
    private PixelMapBuilder map;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized || !Application.isPlaying) return;
        // Clear the temporary Scene-view objects created by the earlier prototype.
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        BuildWorld();
        fishTextures = new[] {
            Resources.Load<Texture2D>("Sprites/Fish/atlantic_salmon"),
            Resources.Load<Texture2D>("Sprites/Fish/common_carp"),
            Resources.Load<Texture2D>("Sprites/Fish/northern_pike")
        };
        initialized = true;
    }

    private void BuildWorld()
    {
        // Clean up only runtime objects created by this prototype before making a fresh test session.
        foreach (var oldPlayer in Object.FindObjectsByType<PixelCharacter>(FindObjectsSortMode.None)) Destroy(oldPlayer.gameObject);
        foreach (var oldMap in GetComponents<PixelMapBuilder>()) Destroy(oldMap);
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 3.6f;
        gameCamera.transform.position = new Vector3(0, 0, -10);
        gameCamera.backgroundColor = new Color(.35f, .60f, .24f);

        map = gameObject.AddComponent<PixelMapBuilder>();
        map.Build();

        var player = new GameObject("Player (Pixel Crawler)");
        player.transform.position = map.PlayerStart;
        character = player.AddComponent<PixelCharacter>();
        character.Setup();
        var follow = gameCamera.gameObject.GetComponent<CameraFollow2D>();
        if (follow == null) follow = gameCamera.gameObject.AddComponent<CameraFollow2D>();
        follow.Target = player.transform;
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
        if (!initialized)
        {
            Initialize();
            if (!initialized) return;
        }
        if (state == State.Explore)
        {
            if (character != null) character.Move();
            bool nearFishingSpot = character != null && Vector2.Distance(character.transform.position, map.FishingSpot) < 1.4f;
            message = nearFishingSpot ? "물가  •  F 키를 눌러 낚시하기" : "이동: WASD 또는 방향키";
            if (nearFishingSpot && Input.GetKeyDown(KeyCode.F)) Cast();
        }
        else if (state == State.Waiting)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) ResetFishing("낚시를 취소했습니다.");
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0) Bite();
        }
        else if (state == State.Reeling) Reel();
        else if (state == State.Result && Input.GetKeyDown(KeyCode.F)) ResetFishing("물가에서 F 키를 눌러 다시 낚시하세요.");
    }

    private void Cast()
    {
        state = State.Waiting; character.SetFishing(true);
        message = "낚싯줄을 던졌습니다… 입질을 기다리는 중  (ESC: 취소)";
        bobber = CreateRect("Bobber", map.BobberSpot, new Vector2(.22f, .22f), new Color(1f, .28f, .2f), 4);
        waitTimer = Random.Range(2f, 6f);
    }

    private void Bite()
    {
        state = State.Reeling; currentFish = fishTextures[Random.Range(0, fishTextures.Length)];
        fishPosition = Random.Range(.15f, .85f); reelPosition = .4f; reelVelocity = 0; fishVelocity = Random.Range(-.15f, .15f);
        progress = escape = 0; message = "입질! 마우스 왼쪽 또는 스페이스를 눌러 흰 영역을 움직이세요.";
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
        if (progress >= 1) ResetFishing("물고기를 잡았습니다! F 키로 다시 낚시할 수 있습니다.");
        else if (escape >= 1) ResetFishing("물고기가 도망갔습니다. F 키로 다시 시도하세요.");
        else if (Input.GetKeyDown(KeyCode.Escape)) ResetFishing("낚시를 취소했습니다.");
    }

    private void ResetFishing(string newMessage)
    {
        if (state == State.Reeling && progress >= 1) result = currentFish == fishTextures[2] ? "노던 파이크" : currentFish == fishTextures[1] ? "잉어" : "대서양 연어";
        state = State.Result; message = newMessage; character.SetFishing(false);
        if (bobber != null) Destroy(bobber);
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, normal = { textColor = Color.white } };
        GUI.Box(new Rect(Screen.width / 2 - 310, 20, 620, 38), ""); GUI.Label(new Rect(Screen.width / 2 - 300, 25, 600, 28), message, style);
        if (state != State.Reeling) { if (!string.IsNullOrEmpty(result)) GUI.Label(new Rect(Screen.width/2-180, 70, 360, 35), "획득: " + result, style); return; }
        var track = new Rect(Screen.width / 2 - 350, Screen.height - 150, 700, 38);
        GUI.Box(track, "");
        float zoneSize = currentFish == fishTextures[2] ? .15f : currentFish == fishTextures[1] ? .20f : .26f;
        GUI.color = Color.white; GUI.Box(new Rect(track.x + reelPosition * track.width, track.y + 3, zoneSize * track.width, track.height - 6), "");
        GUI.color = Color.white;
        if (currentFish != null) GUI.DrawTexture(new Rect(track.x + fishPosition * track.width - 16, track.y - 10, 32, 32), currentFish, ScaleMode.ScaleToFit, true);
        GUI.Box(new Rect(track.x, track.y + 55, track.width * progress, 16), ""); GUI.Label(new Rect(track.x, track.y + 75, 200, 24), "포획 " + Mathf.RoundToInt(progress * 100) + "%");
        GUI.Box(new Rect(track.x + track.width - track.width * escape, track.y + 55, track.width * escape, 16), ""); GUI.Label(new Rect(track.x + 500, track.y + 75, 200, 24), "도주 " + Mathf.RoundToInt(escape * 100) + "%", style);
    }
}
