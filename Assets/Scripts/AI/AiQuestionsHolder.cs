using System.Collections.Generic;
using UnityEngine;

public static class AIQuestionsHolder
{
    public static List<Question> GetAIQuestions()
    {
        List<Question> aiQuestions = new List<Question>
        {
            new Question
            {
                questionText = "What does \"Artificial Intelligence\" (AI) mean?",
                answerOptions = new string[] { "Machines that repair themselves", "Programs that can think like humans", "Computers without power supply", "Robots with feelings" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "What does the abbreviation \"AI\" stand for?",
                answerOptions = new string[] { "Small Innovation", "No Idea", "Artificial Intelligence", "Controlled Information" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is a typical example of weak AI?",
                answerOptions = new string[] { "A robot with its own consciousness", "A calculator", "A chess computer", "A weather app" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is the goal of strong AI?",
                answerOptions = new string[] { "Saving electricity", "Solving a task particularly quickly", "Thinking and acting like a human", "Only writing invoices" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "Where do we encounter AI in everyday life?",
                answerOptions = new string[] { "In bookshelves", "When brushing teeth", "In navigation apps", "When falling asleep" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is weak AI particularly good at?",
                answerOptions = new string[] { "Thinking independently", "Solving problems creatively", "Performing a specific task", "Showing emotions" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "Why is strong AI currently only theoretical?",
                answerOptions = new string[] { "Because it's too expensive", "Because nobody wants it", "Because there is no data", "Because it has not yet been developed" },
                correctAnswerIndex = 3
            },
            new Question
            {
                questionText = "What does AI need to learn?",
                answerOptions = new string[] { "Wi-Fi", "Electricity", "Data", "Sun" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is the learning process of an AI called?",
                answerOptions = new string[] { "Machine Thinking", "Machine Learning", "Artificial Reading", "Robo-Knowledge" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "What is a risk with AI?",
                answerOptions = new string[] { "Bad music", "Long loading times", "Wrong decisions due to faulty data", "Too many colors" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "Where can AI help?",
                answerOptions = new string[] { "With oversleeping", "In medicine", "With cheating", "With baking without an oven" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "What can AI do in education?",
                answerOptions = new string[] { "Replace teachers", "Forbid learning", "Adapt learning material", "Delete books" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "Which statement about AI is correct?",
                answerOptions = new string[] { "AI is always neutral", "AI thinks like a human", "AI can be influenced by data", "AI doesn't need programming" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is an ethical problem with AI?",
                answerOptions = new string[] { "It talks too much", "It chooses favorite people", "It could monitor people", "It doesn't take breaks" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What does a recommendation system like on Netflix show?",
                answerOptions = new string[] { "Random series", "The most expensive movies", "Content that might interest you", "Black and white movies" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is a Deepfake?",
                answerOptions = new string[] { "A cool song", "A fake video created by AI", "A weather forecast", "A computer virus" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "Which form of AI already exists today?",
                answerOptions = new string[] { "Strong AI", "Conscious AI", "Weak AI", "Dream AI" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What can happen if AI uses biased data?",
                answerOptions = new string[] { "It gets slower", "It makes wrong decisions", "It forgets everything", "It turns itself off" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "Why is transparency important in AI?",
                answerOptions = new string[] { "So it looks nicer", "So we understand how it decides", "So it saves power", "So it's quiet" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "What does automation through AI mean?",
                answerOptions = new string[] { "People work faster", "Machines automatically take over tasks", "Everything works without electricity", "Robots drive cars" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "Which professions are particularly at risk from AI?",
                answerOptions = new string[] { "Programmers", "Artists", "Jobs with simple, repetitive tasks", "Teachers" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "Who is responsible for the use of AI?",
                answerOptions = new string[] { "The machine", "Nobody", "The developers and society", "Only the users" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "Which technology helps AI with language understanding?",
                answerOptions = new string[] { "Laser printer", "Text recognition", "Language model", "Microchip" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "Why is AI helpful in research?",
                answerOptions = new string[] { "It can think faster than humans", "It knows all the answers", "It can analyze complex data sets", "It asks questions" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "How can we use AI positively?",
                answerOptions = new string[] { "Apply without control", "Blindly trust", "Use with rules and responsibility", "Only use in games" },
                correctAnswerIndex = 2
            }
        };

        return aiQuestions;
    }
}