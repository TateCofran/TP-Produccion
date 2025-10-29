using System.Collections;
using UnityEngine.Formats.Alembic.Importer;

public interface IAlembicPlayer
{
    IEnumerator Play(AlembicStreamPlayer player, float fallbackSeconds);
}