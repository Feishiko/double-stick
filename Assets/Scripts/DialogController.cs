using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text textLabel;
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private AudioSource typeAudioSource;

    [Header("Typewriter")]
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float minPitch = 0.92f;
    [SerializeField] private float maxPitch = 1.08f;
    [SerializeField] private float volume = 0.6f;

    private readonly List<DialogLine> lines = new List<DialogLine>();
    private int lineIndex;
    private Coroutine typingRoutine;
    private bool isTyping;
    private bool finished;
    private Action onComplete;
    private string currentText = string.Empty;
    private AudioClip currentTypeSound;

    private void Awake()
    {
        if (textLabel == null)
        {
            textLabel = FindText(transform, "Canvas/Image/Text (Legacy)");
        }

        if (leftPortrait == null)
        {
            leftPortrait = FindImage(transform, "Canvas/StandL");
        }

        if (rightPortrait == null)
        {
            rightPortrait = FindImage(transform, "Canvas/StandR");
        }

        if (typeAudioSource == null)
        {
            typeAudioSource = GetComponent<AudioSource>();
            if (typeAudioSource == null)
            {
                typeAudioSource = gameObject.AddComponent<AudioSource>();
            }

            typeAudioSource.playOnAwake = false;
        }
    }

    public void Play(List<DialogLine> dialogLines, Action onCompleted)
    {
        lines.Clear();
        if (dialogLines != null)
        {
            lines.AddRange(dialogLines);
        }

        onComplete = onCompleted;
        lineIndex = 0;
        finished = false;

        if (lines.Count == 0)
        {
            Finish();
            return;
        }

        ShowLine(lines[0]);
    }

    private void Update()
    {
        if (finished)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Advance();
        }
    }

    private void Advance()
    {
        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        lineIndex++;
        if (lineIndex >= lines.Count)
        {
            Finish();
            return;
        }

        ShowLine(lines[lineIndex]);
    }

    private void ShowLine(DialogLine line)
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        currentText = line != null ? line.text : string.Empty;
        currentText = currentText ?? string.Empty;
        currentTypeSound = line != null ? line.typeSound : null;

        ApplyPortrait(leftPortrait, line != null ? line.leftPortrait : null);
        ApplyPortrait(rightPortrait, line != null ? line.rightPortrait : null);

        if (textLabel != null)
        {
            textLabel.text = string.Empty;
        }

        typingRoutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        isTyping = true;

        float interval = 1f / Mathf.Max(1f, charactersPerSecond);
        int shown = 0;

        while (shown < currentText.Length)
        {
            shown++;

            if (textLabel != null)
            {
                textLabel.text = currentText.Substring(0, shown);
            }

            if (currentTypeSound != null && typeAudioSource != null && !char.IsWhiteSpace(currentText[shown - 1]))
            {
                typeAudioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                typeAudioSource.PlayOneShot(currentTypeSound, volume);
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        isTyping = false;
        typingRoutine = null;
    }

    private void CompleteTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;

        if (textLabel != null)
        {
            textLabel.text = currentText;
        }
    }

    private void ApplyPortrait(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.gameObject.SetActive(sprite != null);
    }

    private void Finish()
    {
        if (finished)
        {
            return;
        }

        finished = true;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;

        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();

        Destroy(gameObject);
    }

    private static Text FindText(Transform root, string path)
    {
        Transform target = root.Find(path);
        return target != null ? target.GetComponent<Text>() : null;
    }

    private static Image FindImage(Transform root, string path)
    {
        Transform target = root.Find(path);
        return target != null ? target.GetComponent<Image>() : null;
    }
}
