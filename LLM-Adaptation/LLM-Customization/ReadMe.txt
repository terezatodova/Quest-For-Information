How to run the app using docker-compose:
    Install docker desktop if necessary.
    Make sure that docker desktop is running.
    Run command: 
        docker-compose up --build -d
    Run command docker-compose logs to see prints

This app will expose PC port 8888.

The application will be accessible at 
http://localhost:8888 -> When accessing the server from the same device


Once you run the docker-compose command look into the logs on Docker Desktop. There click on the published URL for the Jupyter lab, drag and drop any files you want to use and start customizing your LLMs. 


The application is intended to run on GPU. 
Make sure that your laptop has an NVIDIA GPU and that your GPU supports CUDA.
Needs a functioning nvidia toolkit - run nvidia-smi to verify. 
If you are on windows you also need to have wls installed and enabled in docker desktop -> https://docs.docker.com/desktop/gpu/
(Also don't forget to run run wsl --update if necessary)