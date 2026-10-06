using System.Collections.Generic;
using UnityEngine;

/// <summary>Playable fishing prototype: move, face the water, cast, wait for a bite, then keep the fish inside the reel zone.</summary>
public sealed class FishingGameController : MonoBehaviour
{
    private enum State { Explore, Waiting, Reeling }
    private State state;
    private PixelCharacter character;
    private Camera gameCamera;
    private GameObject bobber;
    private List<FishDefinition> fishCatalog;
    private FishDefinition currentFish;
    private float waitTimer;
    private float reelPosition = .4f;
    private float reelVelocity;
    private float fishPosition = .55f;
    private float fishVelocity;
    private float directionTimer;
    private float progress;
    private float escape;
    private float resultTimer;
    private string message = "이동: WASD 또는 방향키";
    private string result = "";
    private bool initialized;
    private PixelMapBuilder map;
    private FishingShopSystem shopSystem;
    private FishMarketSystem fishMarketSystem;
    private FishInventory fishInventory;

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
        fishCatalog = FishCatalog.CreateDefault();
        BuildWorld();
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
        var wallet = player.AddComponent<PlayerWallet>();
        wallet.Initialize(1000);
        var inventory = player.AddComponent<FishingRodInventory>();
        fishInventory = player.AddComponent<FishInventory>();

        shopSystem = gameObject.AddComponent<FishingShopSystem>();
        shopSystem.Setup(character, transform.Find("Art Decorations/Fishing Shop"), wallet, inventory);
        fishMarketSystem = gameObject.AddComponent<FishMarketSystem>();
        fishMarketSystem.Setup(character, transform.Find("Art Decorations/Fish Market"), wallet, fishInventory, fishCatalog);

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

        if (resultTimer > 0f)
        {
            resultTimer -= Time.deltaTime;
            if (resultTimer <= 0f)
            {
                resultTimer = 0f;
                result = "";
            }
        }

        if (state == State.Explore)
        {
            if ((shopSystem != null && shopSystem.IsOpen) || (fishMarketSystem != null && fishMarketSystem.IsOpen)) return;
            if (character != null) character.Move();
            if (shopSystem != null && shopSystem.IsPlayerInRange)
            {
                message = "F - 상점 열기";
                return;
            }
            if (fishMarketSystem != null && fishMarketSystem.IsPlayerInRange)
            {
                message = "F - 물고기 판매";
                return;
            }
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
    }

    private void Cast()
    {
        state = State.Waiting; character.SetFishing(true);
        result = "";
        resultTimer = 0f;
        message = "낚싯줄을 던졌습니다… 입질을 기다리는 중  (ESC: 취소)";
        bobber = CreateRect("Bobber", map.BobberSpot, new Vector2(.22f, .22f), new Color(1f, .28f, .2f), 4);
        waitTimer = Random.Range(2f, 6f);
    }

    private void Bite()
    {
        if (fishCatalog == null || fishCatalog.Count == 0)
        {
            ResetFishing("물고기 정보를 불러오지 못했습니다.");
            return;
        }
        state = State.Reeling; currentFish = fishCatalog[Random.Range(0, fishCatalog.Count)];
        fishPosition = Random.Range(.15f, .85f); reelPosition = .4f; reelVelocity = 0; fishVelocity = Random.Range(-.15f, .15f);
        directionTimer = Random.Range(currentFish.DirectionChangeInterval * .65f, currentFish.DirectionChangeInterval * 1.35f);
        progress = escape = 0; message = "입질! 마우스 왼쪽 또는 스페이스를 눌러 흰 영역을 움직이세요.";
        if (bobber != null) bobber.GetComponent<SpriteRenderer>().color = Color.yellow;
    }

    private void Reel()
    {
        float input = (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) ? 1f : -1f;
        reelVelocity += input * 1.35f * Time.deltaTime;
        reelVelocity *= Mathf.Exp(-4f * Time.deltaTime);
        reelPosition = Mathf.Clamp01(reelPosition + reelVelocity * Time.deltaTime);
        fishVelocity += Mathf.Sin(Time.time * 3.1f) * currentFish.MovementAcceleration * Time.deltaTime;
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
        {
            fishVelocity += Random.Range(-currentFish.DirectionImpulse, currentFish.DirectionImpulse);
            directionTimer = Random.Range(currentFish.DirectionChangeInterval * .65f, currentFish.DirectionChangeInterval * 1.35f);
        }
        fishVelocity = Mathf.Clamp(fishVelocity, -currentFish.MaxSpeed, currentFish.MaxSpeed);
        fishPosition += fishVelocity * Time.deltaTime;
        if (fishPosition < .02f || fishPosition > .98f) fishVelocity *= -1f;
        fishPosition = Mathf.Clamp(fishPosition, .02f, .98f);
        float zoneSize = currentFish.ReelZoneSize;
        bool aligned = fishPosition >= reelPosition && fishPosition <= reelPosition + zoneSize;
        progress = Mathf.Clamp01(progress + (aligned ? currentFish.CatchRate : -.12f) * Time.deltaTime);
        escape = Mathf.Clamp01(escape + (aligned ? -.14f : currentFish.EscapeRate) * Time.deltaTime);
        if (progress >= 1) ResetFishing("물고기를 잡았습니다! F 키로 다시 낚시할 수 있습니다.");
        else if (escape >= 1) ResetFishing("물고기가 도망갔습니다. F 키로 다시 시도하세요.");
        else if (Input.GetKeyDown(KeyCode.Escape)) ResetFishing("낚시를 취소했습니다.");
    }

    private void ResetFishing(string newMessage)
    {
        bool caughtFish = state == State.Reeling && progress >= 1f;
        result = caughtFish && currentFish != null ? currentFish.DisplayName : "";
        if (caughtFish && currentFish != null && fishInventory != null) fishInventory.Add(currentFish.Id);
        resultTimer = caughtFish ? 3f : 0f;
        state = State.Explore;
        message = newMessage;
        character.SetFishing(false);
        if (bobber != null) Destroy(bobber);
        bobber = null;
    }

    private void OnGUI()
    {
        if ((shopSystem != null && (shopSystem.IsOpen || shopSystem.IsPlayerInRange)) ||
            (fishMarketSystem != null && (fishMarketSystem.IsOpen || fishMarketSystem.IsPlayerInRange))) return;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, normal = { textColor = Color.white } };
        GUI.Box(new Rect(Screen.width / 2 - 310, 20, 620, 38), ""); GUI.Label(new Rect(Screen.width / 2 - 300, 25, 600, 28), message, style);
        if (state != State.Reeling) { if (!string.IsNullOrEmpty(result)) GUI.Label(new Rect(Screen.width/2-180, 70, 360, 35), "획득: " + result, style); return; }
        var track = new Rect(Screen.width / 2 - 350, Screen.height - 150, 700, 38);
        GUI.Box(track, "");
        float zoneSize = currentFish != null ? currentFish.ReelZoneSize : .2f;
        GUI.color = Color.white; GUI.Box(new Rect(track.x + reelPosition * track.width, track.y + 3, zoneSize * track.width, track.height - 6), "");
        GUI.color = Color.white;
        if (currentFish?.Icon != null) GUI.DrawTexture(new Rect(track.x + fishPosition * track.width - 16, track.y - 10, 32, 32), currentFish.Icon, ScaleMode.ScaleToFit, true);
        GUI.Box(new Rect(track.x, track.y + 55, track.width * progress, 16), ""); GUI.Label(new Rect(track.x, track.y + 75, 200, 24), "포획 " + Mathf.RoundToInt(progress * 100) + "%");
        GUI.Box(new Rect(track.x + track.width - track.width * escape, track.y + 55, track.width * escape, 16), ""); GUI.Label(new Rect(track.x + 500, track.y + 75, 200, 24), "도주 " + Mathf.RoundToInt(escape * 100) + "%", style);
    }
}
