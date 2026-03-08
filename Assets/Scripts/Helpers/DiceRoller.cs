using UnityEngine; // Needed for Random.Range

public static class DiceRoller
{
    /// <summary>
    /// Rolls a single 6-sided die.
    /// </summary>
    /// <returns>An integer between 1 and 6.</returns>
    public static int RollD6()
    {
        // Random.Range(min, max) for integers is exclusive for the max value,
        // so 1, 7 will return values from 1 up to (but not including) 7.
        return Random.Range(1, 7);
    }

    /// <summary>
    /// Rolls a specified number of 6-sided dice and returns their sum.
    /// </summary>
    /// <param name="numDice">The number of dice to roll.</param>
    /// <returns>The sum of the dice rolls.</returns>
    public static int Roll(int numDice)
    {
        int total = 0;
        for (int i = 0; i < numDice; i++)
        {
            total += RollD6();
        }
        return total;
    }
}
