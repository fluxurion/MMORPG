//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. Each Sheet forms a Struct definition, and the name of the Sheet is used as the name of the Struct
// 2. Table convention: the first row is the variable name, the second row is the variable type

// Generate From SpawnDefine.xlsx

public class SpawnDefine
{
	public int ID; // ID
	public int MapId; // Map ID
    public string Pos; // Spawn monster location
    public string Dir; // Direction of spawning monsters
    public int UnitID; // Unit type
    public int Level; // unit level
    public int Period; // Refresh cycle (seconds)
    public string killRewardList; // Kill reward
    public float WalkRange; // patrol range
    public float ChaseRange; // Pursuit range
    public float AttackRange; // Attack range
}


// End of Auto Generated Code
