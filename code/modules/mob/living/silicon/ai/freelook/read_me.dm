// CREDITS
/*
 Initial code credit for this goes to Uristqwerty.
 Debugging, functionality, all comments and porting by Giacom.

 Everything about freelook (or what we can put in here) will be stored here.


 WHAT IS THIS?

 This is a replacement for the current camera movement system, of the AI. Before this, the AI had to move between cameras and could
 only see what the cameras could see. Not only this but the cameras could see through walls, which created problems.
 With this, the AI controls an "AI Eye" mob, which moves just like a ghost; such as moving through walls and being invisible to players.
 The AI's eye is set to this mob and then we use a system (explained below) to determine what the cameras around the AI Eye can and
 cannot see. If the camera cannot see a turf, it will black it out, otherwise it won't and the AI will be able to see it.
 This creates several features, such as.. no more see-through-wall cameras, easier to control camera movement, easier tracking,
 the AI only being able to track mobs which are visible to a camera, only trackable mobs appearing on the mob list and many more.


 HOW IT WORKS

 It works by first creating a camera network datum. Inside of this camera network are "chunks" (which will be
 explained later) and "cameras". The cameras list is kept up to date by obj/machinery/camera/New() and Del().

 Next the camera network has chunks. These chunks are a 16x16 tile block of turfs and cameras contained inside the chunk.
 These turfs are then sorted out based on what the cameras can and cannot see. If none of the cameras can see the turf, inside
 the 16x16 block, it is listed as an "obscured" turf. Meaning the AI won't be able to see it.


 HOW IT UPDATES

 The camera network uses a streaming method in order to effeciently update chunks. Since the server will have doors opening, doors closing,
 turf being destroyed and other lag inducing stuff, we want to update it under certain conditions and not every tick.

 The chunks are not created straight away, only when an AI eye moves into it's area is when it gets created.
 One a chunk is created, when a non glass door opens/closes or an opacity turf is destroyed, we check to see if an AI Eye is looking in the area.
 We do this with the "seenby" list, which updates everytime an AI is near a chunk. If there is an AI eye inside the area, we update the chunk
 that the changed atom is inside and all surrounding chunks, since a camera's vision could leak onto another chunk. If there is no AI Eye, we instead
 flag the chunk to update whenever it is loaded by an AI Eye. This is basically how the chunks update and keep it in sync. We then add some lag reducing
 measures, such as an UPDATE_BUFFER which stops a chunk from updating too many times in a certain time-frame, only updating if the changed atom was blocking
 sight; for example, we don't update glass airlocks or floors.


 WHERE IS EVERYTHING?

 cameranet.dm	=	Everything about the cameranet datum.
 chunk.dm		=	Everything about the chunk datum.
 eye.dm			=	Everything about the AI and the AIEye.
 updating.dm	=	Everything about triggers that will update chunks.

*/
