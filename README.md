# DemoOfVN - Visual Novel Demo Project

Welcome to the **DemoOfVN** project! This repository contains a static demo site showcasing our visual novel project, as well as instructions for running the full Unity project.

![Project Screenshot](https://via.placeholder.com/600x400.png?text=DemoOfVN+Screenshot)  
*Example screenshot of the visual novel interface*

## Static Demo Site
Explore the static version of our visual novel demo here:  
[Static Demo Site](https://github.com/ZifanYE/DemoOfVN_static)

## Full Unity Project
To experience the complete interactive visual novel, you can download and set up the full Unity project.

### Download
Download the full Unity project files from Google Drive:  
[Full Unity Project](https://drive.google.com/file/d/1amnWr7qdqqe1LRTjIMl9jhEM21vGfAhb/view?usp=sharing)

### Prerequisites
- **Unity Version**: Ensure you have **Unity 2022.3.62f1** installed. You can download it from the [Unity Hub](https://unity.com/download).
- **OpenAI API Keys**: You will need two separate OpenAI API keys for the scripts used in the project.

### Setup Instructions
1. **Download and Install Unity**:
   - Install **Unity 2022.3.62f1** using Unity Hub.
   - Extract the downloaded project files from the Google Drive link.

2. **Open the Project in Unity**:
   - Launch Unity Hub and open the extracted project folder.
   - In the Unity Editor, navigate to the **Scenes** folder and open the **AI-chat** scene.

3. **Configure API Keys**:
   - In the Unity Editor's **Hierarchy**, locate the **Core** object.
   - Select the **Multi_choice** object and find the **MultiImageGenerationService** and **AIStoryBrancher** scripts under the **Image_Core** component.
   - In the **Inspector**, locate the `api_key` fields for both scripts:
     - Enter your first **OpenAI API key** in the `api_key` field of the **MultiImageGenerationService** script.
     - Enter your second **OpenAI API key** in the `api_key` field of the **AIStoryBrancher** script.

4. **Add Your Story Content and Images**:
   - In the **AIStoryBrancher** and **MultiImageGenerationService** scripts, find the `Raw Story Text File` field.
   - Provide the path to your story text file in this field. This file should contain the narrative content for your visual novel.
   - Place all required images in the `Assets/Images` folder (or the specific folder designated in your project). The scripts will read images from this folder.

5. **Run the Project**:
   - Press the **Play** button in the Unity Editor to test the project.
   - Ensure the images and story text are correctly loaded and displayed as expected.

### Notes
- Ensure all required image assets are placed in the `Assets/Images` folder (or the appropriate folder specified in your project) for the scripts to read them correctly.
- Double-check that the OpenAI API keys are valid and have the necessary permissions for the services used in the scripts.
- If you encounter issues, verify that you are using the correct Unity version (2022.3.62f1) and that all file paths are correctly set.

## Contributing
Feel free to fork this repository and contribute to the project! If you have suggestions or improvements, please submit a pull request or open an issue.

## License
This project is licensed under the [MIT License](LICENSE). See the LICENSE file for details.

## Contact
For questions or support, please open an issue in the GitHub repository or contact the project maintainer.
