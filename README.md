# ExcavateFabricate

 Terrible clone of a certain game
 

## Controls

WASD = Movement  
Space = Jump  
Hold LMB = Destroy block  
Hold RMB = Preview building block  
RMB + LMB = Build block  
F5 = Save game  
F9 = Load game  

## Possible optimizations

- Merge blocks in chunk into a single mesh - already done, necessary
- Build meshes only for the visible faces of blocks
- Detect neighbouring blocks between chunks - currently only inside chunk
- Create chunk meshes in a shader

## Other possible improvements
- Unit tests
- Refactor code into more classes, PlayerScript especially is messy
- Make a custom shader for chunks, currently using particle shader because it handles mesh colors
- Maybe use Unity Jobs instead of async/await, as that seems to be discouraged

## Other notes
- Infinite range is intentional, makes it a bit more fun in current state
- Seeing chunks appear one by one when loading is also intentional, I found it looking nice
