using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attach this to any GameObject.
/// Drag the dice prefab instance(s) into Dice Group in the Inspector.
/// Call RollDice() from a UI Button's OnClick, or press the configured roll key.
/// Listen to OnRollResult(int) to receive the total of all dice faces.
/// </summary>
public class AnimatedDiceRoller : MonoBehaviour
{
    [Header("Dice")]
    [Tooltip("Drag the dice GameObjects here (each needs a Rigidbody + DiceStats).")]
    [SerializeField] List<GameObject> diceGroup = new List<GameObject>();

    [Header("Roll Settings")]
    [Tooltip("Upward force applied on roll.")]
    [SerializeField] float upForce     = 500f;
    [Tooltip("Random spin torque magnitude.")]
    [SerializeField] float torqueForce = 300f;
    [Tooltip("Keyboard shortcut (e.g. \"space\", \"r\").")]
    [SerializeField] string rollKey    = "space";
    [Tooltip("Seconds before reading the dice faces after rolling.")]
    [SerializeField] float resultDelay = 2f;

    [Header("Events")]
    /// <summary>Fired after each roll; parameter = total of all dice faces.</summary>
    public UnityEvent<int> OnRollResult;

    bool _rolling;

    void Update()
    {
        if (!_rolling && Input.GetKeyDown(rollKey))
            RollDice();
    }

    /// <summary>
    /// Launches all dice in the group.  Wire this to a UI Button's OnClick event.
    /// </summary>
    public void RollDice()
    {
        if (_rolling) return;
        StartCoroutine(RollRoutine());
    }

    IEnumerator RollRoutine()
    {
        _rolling = true;

        foreach (GameObject die in diceGroup)
        {
            if (die == null) continue;

            Rigidbody rb = die.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning($"[AnimatedDiceRoller] {die.name} has no Rigidbody – skipping.");
                continue;
            }

            // Clear previous motion and spin
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            die.transform.rotation = Random.rotation;

            rb.AddForce(Vector3.up * upForce);
            rb.AddTorque(new Vector3(
                Random.Range(-1f, 1f) * torqueForce,
                Random.Range(-1f, 1f) * torqueForce,
                Random.Range(-1f, 1f) * torqueForce));
        }

        // Wait for dice to settle
        yield return new WaitForSeconds(resultDelay);

        int total = 0;
        foreach (GameObject die in diceGroup)
        {
            if (die == null) continue;
            DiceStats stats = die.GetComponent<DiceStats>();
            if (stats != null)
                total += stats.side;
        }

        OnRollResult?.Invoke(total);
        Debug.Log($"[AnimatedDiceRoller] Roll result: {total}");

        _rolling = false;
    }
}
