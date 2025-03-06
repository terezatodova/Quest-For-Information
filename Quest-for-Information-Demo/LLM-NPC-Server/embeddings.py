'''
Handles all logic behind embeddings
'''
from sentence_transformers import SentenceTransformer
from sklearn.metrics.pairwise import cosine_similarity
import app
import os
import torch


'''
Define basic variables like chatbot path or token if needed.
TODO -> Define system prompt, change to loading from file :)
'''
embeddingModelPath: str = "sentence-transformers/all-MiniLM-L6-v2"
device: str =  "cuda" if torch.cuda.is_available() else "cpu"
embeddingModel = None
embeddingsFolder = "./QuestEmbeddings"

'''An embedding dictionary - it's structure is {name: {quest number: {positive/negative: embeddingsList}}}:'''
embeddingsDict = {}

def LoadEmbeddings():
    global embeddingModel

    print("Starting embeddings setup.")
    if (device == "cpu"):
        print("GPU unavailable. Chatbot running on CPU.")
    
    embeddingModel = SentenceTransformer(embeddingModelPath).to(device)

    GenerateEmbeddings()
    print("Embeddng set up finished.")

'''
Parses the input files in the folder QuestEmbeddings
The input files must follow the structure ChatbotName -> quest number -> Positive.txt, negative.txt
Some files can be empty if there is no need for them.
''' 
def GenerateEmbeddings():
    """Load and create embeddings dictionary from a given folder structure."""
    global embeddingsDict

    for nameFolder in os.listdir(embeddingsFolder):
        nameLower = nameFolder.lower()
        namePath = os.path.join(embeddingsFolder, nameFolder)

        if os.path.isdir(namePath):
            embeddingsDict[nameLower] = {}

            for numberFolder in os.listdir(namePath):
                numberPath = os.path.join(namePath, numberFolder)

                if os.path.isdir(numberPath) and numberFolder.isdigit():
                    intNumberFolder = int(numberFolder)
                    embeddingsDict[nameLower][intNumberFolder] = {}

                    for sentimentFile in os.listdir(numberPath):
                        print(sentimentFile)
                        sentimentPath = os.path.join(numberPath, sentimentFile)
                        sentimentKey = 'positive' if sentimentFile.lower()=='positive.txt' else 'negative'

                        if os.path.isfile(sentimentPath):
                            with open(sentimentPath, 'r', encoding='utf-8') as f:
                                lines = f.readlines()
                                embeddingsList = [(line.strip(), GetEmbedding(line.strip())) for line in lines]
                                embeddingsDict[nameLower][intNumberFolder][sentimentKey] = embeddingsList

'''
Creates and returns an embedding of the input text
'''
def GetEmbedding(text):
    return embeddingModel.encode(text)

'''
Searches for a match to the user prompt from the type.txt file (positive/negative) of chatbotName and questNum
Returns the match if it was found, empty string if not
'''
def FindBestMatch(prompt, chatbotName, questNum, type, similarityTreshold = 0.8):
    if (chatbotName is None or questNum is None):
        return ""

    try:
        inputEmbedding = GetEmbedding(prompt)
        bestMatch = ""
        positiveEmbeddingList = embeddingsDict[chatbotName][questNum][type]

        for question, questionEmbedding in positiveEmbeddingList:
            similarity = cosine_similarity([inputEmbedding], [questionEmbedding])[0][0]
            app.app.logger.info(f"Sentence from user: {prompt} compared to {question} has similarity {similarity}")
            if similarity > similarityTreshold:
                bestMatch = question
                similarityTreshold = similarity

        app.app.logger.info(f"Sentence from user: {prompt} has best match on {type} : {bestMatch}")
        return bestMatch
    except KeyError as e:
        app.app.logger.error(f"Missing embeddings for chatbotName: {chatbotName}, questNum: {questNum}, type: {type}. Error: {e}")
        return ""
    except Exception as e:
        app.app.logger.error(f"An unexpected error occurred: {e}")
        return ""