using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SoundPulse : MonoBehaviour
{
    [Tooltip("How long the pulse takes, in seconds")]
    public float duration = 0.5f;

    [Tooltip("How big (in local-space units) the circle will grow")]
    public float maxScale = 5f;

    SpriteRenderer _sr;
    float          _elapsed;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        // start invisible & tiny
        transform.localScale = Vector3.zero;
        _sr.color = new Color(1,1,1,1);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / duration);

        // 1) scale from 0 → maxScale
        float s = Mathf.Lerp(0, maxScale, t);
        transform.localScale = new Vector3(s, s, 1);

        // 2) fade alpha 1 → 0
        var c = _sr.color;
        c.a = Mathf.Lerp(1, 0, t);
        _sr.color = c;

        // 3) destroy when done
        if (t >= 1f) Destroy(gameObject);
    }
}
