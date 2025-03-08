using UnityEngine;

[System.Serializable]
public class Question
{
    public string questionText;
    public string[] answerOptions = new string[4];
    public int correctAnswerIndex;
}