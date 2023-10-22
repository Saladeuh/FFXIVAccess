using System.Collections;
using System.Collections.Generic;
using GameQuestManager = FFXIVClientStructs.FFXIV.Client.Game.QuestManager;

namespace Mappy.DataModels;

public class QuestList : IEnumerable<QuestExtended>
{
    public IEnumerator<QuestExtended> GetEnumerator() => new QuestListEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public unsafe class QuestListEnumerator : IEnumerator<QuestExtended>
{
    private byte* GameQuestList => GameQuestManager.Instance()->NormalQuests;
    private int currentPosition = -1;
    private const int Maximum = 30;

    public bool MoveNext()
    {
        currentPosition++;
        return currentPosition < Maximum;
    }

    public void Reset() => currentPosition = -1;

    public QuestExtended Current => *(QuestExtended*)GameQuestList[currentPosition];

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}
