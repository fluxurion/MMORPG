//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. Each Sheet forms a Struct definition, and the Sheet name is used as the Struct name
// 2. Table convention: The first row is the variable name, the second row is the variable type

// Generate From DialogueDefine.xlsx

public class DialogueDefine
{
    public int ID; // ID
    public string Decs; // Description
    public string Content; // Content
    public string Options; // Option ID list
    public int Jump; // Jump
    public string AcceptTask; // Accepted task ID, dialogue ID to jump to if acceptance fails
    public string SubmitTask; // Submitted task ID, dialogue ID to jump to if submission fails
    public int SaveDialogueId; // Used when NPC speaks
    public string TipResource; // Overhead tip: question, exclamation, asterisk
}


// End of Auto Generated Code
