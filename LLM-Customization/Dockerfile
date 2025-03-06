FROM nvidia/cuda:12.1.0-base-ubuntu22.04

ENV DEBIAN_FRONTEND=noninteractive

WORKDIR /workspace
COPY . /workspace

RUN apt-get update && apt-get install -y \
    gcc \
    g++ \
    python3.11 \
    python3.11-distutils \
    python3-pip \
    curl \
    git \
    && apt-get clean

RUN pip3 install --upgrade pip
RUN pip install torch==2.3.0+cu121 torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
RUN pip3 install -r requirements.txt
RUN pip3 install jupyterlab

EXPOSE 8888

CMD ["jupyter-lab", "--ip=0.0.0.0", "--no-browser", "--allow-root"]