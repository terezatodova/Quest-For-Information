'''
The main application responsible for setting up and running the server and managing endpoints.
'''
import chatbot
import embeddings
from flask import Flask, request, jsonify
from flask_cors import CORS
import logging


app = Flask(__name__)
CORS(app)

'''
Initialize the app - load the chatbot model and embeddings
'''
def Initialize():
    logging.basicConfig(level=logging.DEBUG)   #Enable logging if you want more info
    chatbot.LoadChatbot()
    embeddings.LoadEmbeddings()

'''
API endpoint for checking the status of th server
Initialize calls are done before the server is created. 
Returns True as soon as the server is up.
'''
@app.route('/status', methods=['GET'])
def Status():
    return jsonify({"Status": True}), 200
    
'''
API endpoint for player-chat interaction.
Will receive the chatbot conversational history as input and return the generated response
The input conversational history must be in the correct format

Expected input:
History -> Dict with the conversation history
ChatbotName -> Unique identifier - chatbot name. Optional, if we want to use embeddings.
Quest -> number representing which quest is currently running. Optional, if we want to use embeddings.
PlayerQuestCompletion -> True by default (the player says something to complete the quest)
                        False if we want the NPC to complete the quest (if NPC says something the quest will be completed)

Expected output:
Response -> response of chatbot
QuestCompletion -> true if the quest was completed, false otherwise
NeedsRefresh -> true if the tokenizer is getting full and the chatbot needs refreshing
'''
@app.route('/chat', methods=['POST'])
def Chat():
    data = request.get_json()

    # Parse conversatoin history as a list of dicts
    conversationHistory = data.get('History', [])
    chatbotName = data.get('ChatbotName', None)
    quest = data.get('Quest', None)
    playerQuestCompletion = data.get('PlayerQuestCompletion', None)

    #app.logger.info("Received conversation history: " + str(conversationHistory))

    if not conversationHistory:
        app.logger.info("error: No conversation history provided. The chatbot should have been provided with a system prompt at least.")
        return 400
    
    if (chatbotName and not quest) or (not chatbotName and quest):
        app.logger.info("error: Either chatbotName or quest is null (but not both). Error in generating response.")
        return 400
    
    if (chatbotName is None or chatbotName == ""):
        response, questCompletion, needsRefresh = chatbot.GenerateErrorResponse(conversationHistory)
    elif (playerQuestCompletion):
        response, questCompletion, needsRefresh = chatbot.GenerateResponse(conversationHistory, chatbotName.lower(), quest)
    else:
        response, questCompletion, needsRefresh = chatbot.GenerateResponseNPCCompletion(conversationHistory, chatbotName.lower(), quest)
        

    if (response is None):
        app.logger.info("error: Conversation history was sent in the wrong format.")
        return 400
    
    
    #app.logger.info("Response generated successfully: " + response)
    return jsonify({"Response": response, "QuestCompletion" : questCompletion, "NeedsRefresh": needsRefresh})

if __name__ == '__main__':
    # The host='0.0.0.0' allows the server to be accessible on local network
    Initialize()
    app.run(host='0.0.0.0', port=5000)