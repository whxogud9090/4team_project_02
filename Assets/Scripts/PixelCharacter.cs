using UnityEngine;

/// <summary>Small runtime sprite animator using the supplied Pixel Crawler sheets (64 x 64 frames).</summary>
public sealed class PixelCharacter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Texture2D idle;
    private Texture2D walk;
    private Texture2D fishing;
    private float frameTime;
    private bool isFishing;
    public float speed = 2.2f;

    public void Setup()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 3;
        idle = Resources.Load<Texture2D>("Sprites/Player/IdleDown");
        walk = Resources.Load<Texture2D>("Sprites/Player/WalkDown");
        fishing = Resources.Load<Texture2D>("Sprites/Player/FishingDown");
        SetFrame(idle, 0);
    }

    public void Move()
    {
        Vector2 movement = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        movement = movement.normalized;
        transform.position += (Vector3)(movement * speed * Time.deltaTime);
        frameTime += Time.deltaTime;
        SetFrame(movement.sqrMagnitude > .01f ? walk : idle, Mathf.FloorToInt(frameTime * (movement.sqrMagnitude > .01f ? 8f : 4f)));
        if (movement.x != 0) spriteRenderer.flipX = movement.x < 0;
    }

    public void SetFishing(bool value) { isFishing = value; frameTime = 0; }

    private void Update()
    {
        if (!isFishing) return;
        frameTime += Time.deltaTime;
        SetFrame(fishing, Mathf.FloorToInt(frameTime * 7f));
    }

    private void SetFrame(Texture2D source, int frame)
    {
        if (source == null) return;
        int count = source.width / 64;
        frame %= count;
        spriteRenderer.sprite = Sprite.Create(source, new Rect(frame * 64, 0, 64, 64), new Vector2(.5f, .1f), 48f);
    }
}
