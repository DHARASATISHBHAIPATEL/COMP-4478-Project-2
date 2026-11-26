Version : Unity 6.5 (6000.5.8f1)

Sprites:
https://kenney.nl/assets/platformer-characters : 2D

https://kenney.nl/assets/blocky-characters : 3D (Downloaded but didn't used)

Sprites: 

Idle: 0
Walk: 9,10 Same for the Run
Jump: 1
Fall: 2
Crouch: 3
Cheer: 7,8
Back view: 22
Climb: 5,6

Crouch, Cheer, Back view and Climb are not used yet but can be added later 

PlayerMovement.cs - walk, run (hold Shift), jump, ground check, player faces left/right
Animation clips: Idle, Walk, Run, Jump, Fall
Animator Controller (Player) that switches between the clips using Speed, isRunning, isGrounded and yVelocity

Resources: 

### Sprites
Unity Manual - Sprite Editor (slicing the sprite sheet): https://docs.unity3d.com/Manual/sprite/sprite-editor/use-editor.html
Lecture 8 - Sprite sheet animation (slicing, Animation window, Samples)

## Player Movement
Lecture 7 - PlayerMovement script (Rigidbody2D, linearVelocity, Input.GetAxis)
Brackeys - 2D Movement in Unity (video): https://youtu.be/dwcT-Dch0bA
Brackeys - 2D Character Controller (used to learn from, not copied): https://github.com/Brackeys/2D-Character-Controller

### Animation and Animator Controller
Create 2D sprite animations: https://learn.unity.com/tutorial/create-2d-sprite-animations
Animation Parameters (Float, Bool, Trigger): https://docs.unity3d.com/Manual/AnimationParameters.html
Brackeys - 2D Animation project files: https://github.com/brackeys/2d-animation
Lecture 8 - Animator Controller (states, parameters, transitions, controlling it from a script)

### 3D model (looked at, not used)
For Importing FBX file: https://docs.unity3d.com/6000.2/Documentation/Manual/ImportingModelFiles.html 





