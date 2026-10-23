using System.Collections.Generic;
using UnityEngine;

public class DialogArea : MonoBehaviour
{
    [Header("Dialog")]
    [SerializeField] private List<DialogLine> dialog = new List<DialogLine>();
    [SerializeField] private GameObject dialogPrefab;

    private bool hasPlayed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryPlay(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryPlay(other);
    }

    private void TryPlay(Collider2D other)
    {
        if (hasPlayed || other == null || other.isTrigger)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerMovement2D>() == null)
        {
            return;
        }

        LevelManager manager = LevelManager.Instance;
        if (manager != null && !manager.IsLevelActive)
        {
            return;
        }

        if (dialog == null || dialog.Count == 0)
        {
            return;
        }

        if (FindObjectOfType<DialogController>() != null)
        {
            return;
        }

        GameObject prefab = ResolveDialogPrefab(manager);
        if (prefab == null)
        {
            Debug.LogWarning("DialogArea: no Dialog prefab configured and no LevelManager available.");
            return;
        }

        hasPlayed = true;

        LevelManager.SetActorsFrozen(true);

        GameObject instanceObject = Instantiate(prefab);
        DialogController controller = instanceObject.GetComponentInChildren<DialogController>(true);
        if (controller == null)
        {
            Destroy(instanceObject);
            LevelManager.SetActorsFrozen(false);
            return;
        }

        controller.Play(dialog, () => LevelManager.SetActorsFrozen(false));
    }

    private GameObject ResolveDialogPrefab(LevelManager manager)
    {
        if (dialogPrefab != null)
        {
            return dialogPrefab;
        }

        return manager != null ? manager.DialogPrefab : null;
    }
}
