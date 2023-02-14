using Lindon.UserManager;
using Lindon.UserManager.Base.Page;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HUDPage : UIPage
{
    [SerializeField] private TMP_Text m_EnemyScoreText;
    [SerializeField] private Button m_PauseButton;

    [Header("Rotate")]
    [SerializeField] private EventTrigger trigger;
    [SerializeField, Min(0)] private float rotateSpeed = 1;
    private Transform roatetTransform;
    private float _rotationVelocity;
    private bool m_ActiveDrag;

    protected override void SetValues()
    {
        m_ActiveDrag = true;
        DisplayEnemyCount();
        Time.timeScale = 1;
        roatetTransform = Tower.Instance.transform;
    }

    protected override void SetValuesOnSceneLoad()
    {
        EnemyCounter.OnKillEnemy += DisplayEnemyCount;

        m_PauseButton.onClick.RemoveAllListeners();
        m_PauseButton.onClick.AddListener(() =>
        {
            /*PAUSE PAGE*/
            UserInterfaceManager.OnBackPressed();
        });

        EventTrigger.Entry dragEntry = new EventTrigger.Entry();
        dragEntry.eventID = EventTriggerType.Drag;
        dragEntry.callback.AddListener((data) => OnDrag((PointerEventData)data));
        trigger.triggers.Add(dragEntry);
    }

    private void OnDisable()
    {
        m_ActiveDrag = false;
    }

    private void OnDestroy()
    {
        EnemyCounter.OnKillEnemy -= DisplayEnemyCount;
    }

    private void DisplayEnemyCount()
    {
        m_EnemyScoreText.SetText($"{EnemyCounter.KilledEnemy}/{EnemyCounter.TotalEnemy}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!m_ActiveDrag) return;

        _rotationVelocity = eventData.delta.x * rotateSpeed;
        roatetTransform.Rotate(Vector3.up, -_rotationVelocity, Space.Self);
    }
}
