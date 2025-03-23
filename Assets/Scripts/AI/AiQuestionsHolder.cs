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
                questionText = "What does AI stand for?",
                answerOptions = new string[] { "Automated Intelligence", "Artificial Intelligence", "Algorithmic Interface", "Automated Interaction" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "Which of these is a type of machine learning?",
                answerOptions = new string[] { "Supervised Learning", "Visual Learning", "Memory Learning", "Physical Learning" },
                correctAnswerIndex = 0
            },
            new Question
            {
                questionText = "What AI technique is inspired by the human brain?",
                answerOptions = new string[] { "Decision Trees", "Random Forest", "Neural Networks", "Regression Analysis" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is 'NLP' in AI?",
                answerOptions = new string[] { "New Learning Process", "Natural Language Processing", "Neural Logic Programming", "Network Learning Protocol" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "Which company developed ChatGPT?",
                answerOptions = new string[] { "Google", "Microsoft", "OpenAI", "Meta" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What does a Generative Adversarial Network (GAN) do?",
                answerOptions = new string[] { "Classifies data", "Generates new content", "Optimizes hardware", "Translates languages" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "Which algorithm is used for pathfinding in games?",
                answerOptions = new string[] { "A* (A-Star)", "K-means", "LSTM", "SVM" },
                correctAnswerIndex = 0
            },
            new Question
            {
                questionText = "What is 'overfitting' in machine learning?",
                answerOptions = new string[] { "When a model trains too quickly", "When a model performs well on training data but poorly on new data", "When a model uses too much memory", "When a model has too many layers" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "Which programming language is most commonly used for AI development?",
                answerOptions = new string[] { "Java", "C++", "Python", "JavaScript" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What does reinforcement learning focus on?",
                answerOptions = new string[] { "Supervised classification", "Finding patterns in data", "Learning through reward and punishment", "Memorizing large datasets" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is a Transformer model known for?",
                answerOptions = new string[] { "Image generation", "Natural language understanding", "Robotic movement", "Speech synthesis" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "Which AI technique is used for image recognition?",
                answerOptions = new string[] { "Regression", "Convolutional Neural Networks", "Decision Trees", "Linear Algebra" },
                correctAnswerIndex = 1
            },
            new Question
            {
                questionText = "What are LLMs in AI?",
                answerOptions = new string[] { "Large Language Models", "Linear Learning Machines", "Logical Learning Methods", "Layered Learning Mechanisms" },
                correctAnswerIndex = 0
            },
            new Question
            {
                questionText = "Which of these is NOT a form of AI?",
                answerOptions = new string[] { "Machine Learning", "Deep Learning", "Blockchain", "Natural Language Processing" },
                correctAnswerIndex = 2
            },
            new Question
            {
                questionText = "What is the Turing Test used for?",
                answerOptions = new string[] { "Testing computer speed", "Evaluating if AI can imitate human behavior", "Checking for bugs in code", "Measuring processing power" },
                correctAnswerIndex = 1
            }
        };

        return aiQuestions;
    }
}