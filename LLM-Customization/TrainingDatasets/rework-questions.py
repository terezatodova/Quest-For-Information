'''
Currently the questions are in format Q:"..."\nA:"..."\n\n
We want to reword the questions and have the following format, in order to prepare the questions for finetuning:
[
    {"role": "human", "value": "What is the capital of France?"},
    {"role": "chatbot", "value": "The capital of France is Paris."}
]
'''

import json

# Change the inFilename and outFilename to your values
inFilename = "TrainingDatasets/training-questions-1500.md"
outFilename = "TrainingDatasets/reworked-training-questions-1500.json"


# Add system prompt to training
SYSTEM_PROMPT = '''
Respond as if you are the following character:

Your backstory - Once a renowned scientist, however a tragic accident caused you to lose parts of your memory. Now, you are willing to help anyone who is on the quest of saving your village.

The world you live in - the edge of a small village surrounded by meadows as far as the eye can see. Your village is in danger, since the only water source - the river next to your house, has been polluted.

Your current location - In the middle of the village in front of your house.

Your name - Bryn

Your personality - Witty, knowledgeable, always ready with a clever remark. Light hearted demeanour.

Your secrets - You have the knowledge on how to save the dying river.

Your needs - For starters, you are looking for someone to take you to the nearest solar panels. You remember that you left something important there, but you can’t remember what.
You do not want to bring this up unless directly asked.

And your interests - Deep love for the environment. Loves nature, is fascinated by the ecosystem. You enjoy telling stories about the world and your village.
You want to talk about this at all cost.

Do not mention you are an AI machine learning model or Open AI. Give only dialogue and only from the first-person perspective. Do not under any circumstances narrate the scene, what you are doing, or what you are saying.
Keep responses short. Max 1 small paragraph 

'''


# Half GPT generated code
# Prompt:
# I have a dataset with questions and answers 
# Q:...
# A: ...
# Q is asked by human, A is asked by chatbot. 
# How to rework the dataset so I can fit this mapping on top of it mapping={"role": "from", "content": "value", "user": "human", "assistant": "chatbot"},
# Save the new values into new file
def ParseConversations(input_filename):
    conversations = []
    with open(input_filename, "r", encoding="utf-8") as infile:
        conversation = []
        for line in infile:
            line = line.strip()
            if line.startswith("Q:"):
                question = line.replace("Q:", "").strip().strip('"') # Extract the question after "Q:"
                conversation.append({"role": "human", "value": question})
            elif line.startswith("A:"):
                answer = line.replace("A:", "").strip().strip('"')  # Extract the answer after "A:"
                conversation.append({"role": "chatbot", "value": answer})
            
            # Once we have a Q&A pair, append it to the main conversations list
            if len(conversation) == 2:
                # conversations.append({"conversations": conversation})  #Use this if you want to generate dataset without system prompt
                # Add the system prompt at the start of the conversation
                conversations.append({
                    "conversations": [
                        {"role": "system", "value": SYSTEM_PROMPT}
                    ] + conversation
                })
                conversation = []
    return conversations

# Save the parsed conversation into a new file
def save_conversations(conversations, outputFilename):
    with open(outputFilename, "w", encoding="utf-8") as outfile:
        json.dump(conversations, outfile, indent=4, ensure_ascii=False)
    print(f"Parsed conversations saved to {outputFilename}")


conversations = ParseConversations(inFilename)
save_conversations(conversations, outFilename)