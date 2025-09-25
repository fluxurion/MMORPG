//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. Each Sheet forms a Struct definition, and the name of the Sheet is used as the name of the Struct
// 2. Table convention: the first row is the variable name, the second row is the variable type

// Generate From DialogueDefine.xlsx

public class DialogueDefine
{
	public int ID; // ID
	public string Decs; // introduce
    public string Content; // content
    public string Options; // option id list
    public int Jump; // Jump
    public string AcceptTask; // Accept the task ID, the dialogue ID to jump to when the task fails
    public string SubmitTask; // Submit task id, the dialog id to jump to when submission fails
    public int SaveDialogueId; // Used when NPC is speaking
    public string TipResource; // Overhead prompts, questions, exclamations, asterisks
}


// End of Auto Generated Code
