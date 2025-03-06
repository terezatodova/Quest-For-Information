'''
Handles all chatbot logic like generating prompt
'''
import embeddings
import torch
import transformers
import app
import re

'''
Define basic variables like chatbot path or token if needed.
Note - tokenTreshold tells us when the server will warn the client that the tokenizer is getting too full
'''
chatbotPipeline = None
maxTokensMessage: int = 256
chatbotModelPath: str = "meta-llama/Llama-3.2-3B-Instruct"
token: str = "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"
tokenTreshold: int = 0.75
maxTokensOverall: int = 128000 #As provided by ai.meta.com

def LoadChatbot():
    print("Starting chatbot setup.")
    global chatbotPipeline

    chatbotPipeline = transformers.pipeline(
        "text-generation",
        model=chatbotModelPath,
        token=token,
        model_kwargs={"torch_dtype": torch.bfloat16},
        device_map="cuda",
    )

    print("Chatbot set up finished.")

# We dont use embeddings here
def GenerateErrorResponse(history):
    app.app.logger.info(f"Generating failure response. History: {history}")

    outputs = chatbotPipeline(
        history,
        max_new_tokens=maxTokensMessage,
    )
    answer = outputs[0]["generated_text"][-1]['content']
    
    return answer, False, False

def GenerateResponse(history, chatbotName, quest):
    prompt, history = PopLastQuestionFromHistory(history)
    questCompletion = False

    # First check whether quest was completed
    bestPositiveMatch = embeddings.FindBestMatch(prompt, chatbotName, quest, "positive", 0.8)
    bestNegativeMatch = embeddings.FindBestMatch(prompt, chatbotName, quest, "negative")

    if (bestPositiveMatch != ""):
        # Modify the prompt 
        prompt = f'''
        The user said: {prompt}
        It is exactly what you needed to hear and you want to thank them. Answer in first person and stay in character.
        '''
        questCompletion = True
    elif (bestNegativeMatch != ""):
        # Modify the prompt 
        prompt = f'''
        The user said: {prompt}
        You don't know any information about this. You can't help the user. Answer in first person and stay in character.
        '''
    else:
        # Modify the prompt to keep consistency
        prompt = f'''
        The user said: {prompt}
        If the user didn't give you the information you needed continue the conversation.
        If the user gave you the information you needed it doesn't feel like it is correct.
        Answer in first person and stay in character. 
        '''

    history.append({"role": "user", "content": prompt})

    app.app.logger.info(f"New history from user: {history}")

    outputs = chatbotPipeline(
        history,
        max_new_tokens=maxTokensMessage,
    )
    answer = outputs[0]["generated_text"][-1]['content']

    #Filter out text in *...* since this implies narration of scene or actions
    answer = ResponseCleanup(answer)

    #Check whether the tokenizer is getting too full
    tokenizer = chatbotPipeline.tokenizer
    total_tokens = sum(len(tokenizer.encode(turn["content"])) for turn in history)
    needsMeemoryRefresh = total_tokens >= maxTokensOverall * tokenTreshold  # Check if total tokens exceed the limit
    
    return answer, questCompletion, needsMeemoryRefresh

# In this case the NPC can complete the quest. We check the NPC response against the positive embeddings
def GenerateResponseNPCCompletion(history, chatbotName, quest):
    questCompletion = False

    app.app.logger.info(f"Expecting cquest completion from llm. Chat history from user: {history}")

    outputs = chatbotPipeline(
        history,
        max_new_tokens=maxTokensMessage,
    )
    answer = outputs[0]["generated_text"][-1]['content']

    #Filter out text in *...* since this implies narration of scene or actions
    answer = ResponseCleanup(answer)

    # First check whether quest was completed against npc answer
    bestPositiveMatch = embeddings.FindBestMatch(answer, chatbotName, quest, "positive", 0.5)

    questCompletion = (bestPositiveMatch != "")
    
    #Check whether the tokenizer is getting too full
    tokenizer = chatbotPipeline.tokenizer
    total_tokens = sum(len(tokenizer.encode(turn["content"])) for turn in history)
    needsMeemoryRefresh = total_tokens >= maxTokensOverall * tokenTreshold  # Check if total tokens exceed the limit
    
    return answer, questCompletion, needsMeemoryRefresh

def PopLastQuestionFromHistory(history):
    lastUserQuestion = None
    
    # Iterate over the history in reverse to find and remove the last user question
    for i in range(len(history) - 1, -1, -1):
        if history[i]['role'] == 'user':
            lastUserQuestion = history.pop(i)['content']
            break

    return lastUserQuestion, history

# Removes the text in asterixes since this typically represents narration
# Also removes text in () for the same reason
def ResponseCleanup(answer):
    filteredAnswer = re.sub(r"\*.*?\*", "", answer)
    filteredAnswer = re.sub(r"\(.*?\)", "", filteredAnswer)
    return " ".join(filteredAnswer.split())
