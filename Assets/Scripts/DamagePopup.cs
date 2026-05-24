using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float lifetime = 0.6f;
    [SerializeField] private float riseSpeed = 1.2f;

    private float timer;

    void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();
    }

    public void SetValue(int damage, Color color)
    {
        if (text != null)
        {
            text.text = damage.ToString();
            text.color = color;
        }
        timer = lifetime;
    }

    void Update()
    {
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        timer -= Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        if (text != null)
        {
            Color c = text.color;
            c.a = t;
            text.color = c;
        }

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
