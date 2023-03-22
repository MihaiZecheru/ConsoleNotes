namespace ConsoleNotes;

public delegate void KeyEventHandler(ConsoleKeyInfo keyinfo);

internal class KeyEvents
{
    private KeyEventHandler func;
    private List<ConsoleKey> TriggerKeys;

    public KeyEvents(KeyEventHandler func, List<ConsoleKey> triggerKeys)
    {
        this.func = func;
        this.TriggerKeys = triggerKeys;
    }

    public void Start()
    {
        ConsoleKeyInfo keyinfo;

        while (true)
        {
            keyinfo = Console.ReadKey(true);

            if (TriggerKeys.Contains(keyinfo.Key))
            {
                func(keyinfo);
            }
        }
    }
}
