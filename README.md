TallyProcessor
==============

This software is published under the GNU GPL 3.0
You find a copy of this licence in the file gpl-3.0.txt under TallyProcessors program directory.  


What is TallyProcessor:
-----------------------

TallyProcessor is a software for sending, receiving and combining tally information of various techniques and devices.
It is designed to abstract the simple tally wireing, how it is done to connect a vision mixer to camera tally, to a logical layer
and add the power of bringing different tally techniques together, defining rulesets to switch tallys based on tally inputs and implement
whole tally networks.  


How does it work:
-----------------

To achive these goals, the straightforward tally topology to wire a vision mixer to a tallylamp or other tally consumers 
is broken into a multi stage workflow where everything is splitted into
* tally producers (i.e. vision mixer)         - providing tally information
* tally consumer (i.e cameras, displays)      - consuming tally information
* devices                                     - connection producers/consumers to TallyProcessor
* channels                                    - a named input or output (which is a consumer/producer) of a device
* mappings                                    - a ruleset which defines at which condtitions a tally output is active
    
In this abstraction, every tally information, further on named channel, is just a boolean state on/off, a unique name and channel number.
To achive tallys with cued/active information, you will use two separat channels.

<pre>
                         rules
                           |
tally producer --> tally processor --> tally consumer
</pre>

A tally output is not strictly related to an input. To make a tally output active, you have to define a ruleset that must be fullfilled
to switch it on. These rulesets can contain a single input, which would be how tally normaly works, or a complex boolean expression
containing inputs, constants and boolean operators.
As an example, lets say we have a vision mixer and two cameras. We want standard tally without cued information.
So we would have 2 inputs and 2 outputs.
|Name|Meaning|
|:---|:---|
|**INPUTS**||
|vmix1|vision mixer channel 1 active|
|vmix2|vision mixer channel 2 active|
|**OUTPUTS**||
|cam1|camera|
|cam2|camera|
	
Now we would define the following mappings for the outputs:
|Name|Mapping|
|:---|:---|
|cam1|vmix1|
|cam2|vmix2|
        
This is pretty simple. So lets extend it a bit.
We're adding a studiolight "on air" to our setting which should be active if one of the cameras is selected at the vision mixer. 
The inputs would stay the same, but we would add an output:
outputs:
|Name|Mapping|
|:---|:---|
|cam1|vmix1|
|cam2|vmix2|
|on air|vmix1 *OR* vmix2|
        
This is still simple but you can extend it as you want and build complex rules using all defined inputs and the operators:
|Operator|Meaning|
|:---|:---|
|*AND*|both inputs must be active to set it active|
|*OR* |one and/or the other input must be active to set it active|
|*XOR*|one or the other but not both inputs must be acitve to set it active|
|*NOT*|the input must be inactive to set it active|

You can combine operators and inputs in a mapping like this:
|Name|Mapping|
|:---|:---|
|output|(input1 OR input2) AND NOT(input3)|
    
Which would be active if one of the inputs 1, 2 are active but input3 inactive.
You can not use outputs in your mappings, only inputs, operators and the constants true/false.

These mappings will be defined in the config file.


Now how we get tally in and out of the computer?
This is were the devices come in.
A device is a part of the TallyProcessor that translates the logic to a specified tally technique.
This can be a hardware board switching and reading open collector ports or a network client listening to
information provided by a modern vision mixer or playout server.

Every tally technique needs a own device. 
There are 3 kinds of devices shipped with TallyProcessor:
* simpleNetwork   -   A network device sending and receiving tally data as xml string to/from a given IP and PORT
* atem            -   A network device using the BlackmagicDesing ATEM SDK to get tally from ATEM vision mixers
* k8055d          -   A dll based device reading and writing tally to a usb board with open collector ports
    
But actualy, you should implement your own devices to get your tally working.
See developer.txt for further information.

The devices have to be configured within the config file. Each device has general parts which are equal no matter what
kind it is of. These are needed information for TallyPorcessors main core. But there may be special setting depending
on the device itself which has to supplied with in the parameter section.

The config file is xml and should be in the "conf/" dir in your app folder.
TallyProcessor will search for a file named config.xml and load it as standard.
But you can also provide a commandline argument giving a relativ path or absolute path to the file.

To understand the config file and its format, please see config.example.xml in the config path.
Not everybody will own a device that is implemented here, so I also provided a simpleNetwork device
which can be run by every computer. It is a network based device sending and receiving tally as xml messages
over tcp/ip connections.
So, if you want to test, have a look at the config.simpleNetwork.xml file in the conf folder.


Requirements:
-------------

TallyProcessor is developed in VB.NET and needs at least the 
NET Framework 4
to run properly.
See http://www.microsoft.com/en-us/download/details.aspx?id=17851

If you want to use the atem device, you need the
ATEM Switcher Software 3.3 (higher/lower Version may work, but haven't been testet)
See http://www.blackmagicdesign.com/products/atem/

For the k8055d USB board device you need the hardware from velleman 
and the k8055d.dll which is shipped with TallyProcessor. 
See http://www.velleman.eu/products/view/?id=404998


FAQ:
----

No FAQ yet.
