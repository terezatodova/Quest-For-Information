# Using CUDA-enabled base image that includes Python
FROM nvidia/cuda:12.1.1-runtime-ubuntu20.04

WORKDIR /app
COPY . /app

RUN apt-get update && apt-get install -y \
    gcc \
    g++ \
    procps \
    python3 \
    python3-pip \
    python3-dev
RUN pip install -r requirements.txt

# Port for the python server
EXPOSE 5000

# Run the LLM chatbot
CMD ["python3", "app.py"]