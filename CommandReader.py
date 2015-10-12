w1387769 Mohammed Rahim Baraky
import time, os, sys, hardware_controller

hardware_controller.initialise()

#This class is used to determine what state we will be 
#transitioning to next. The Constructor takes in a string
#containing the name of the next state E.G Idle or move
class Transition(object):
	def __init__(self,toState):
		self.toState= toState
#######################################################################
##States
#This section contains all the different states we will have
#and the code they execute when where in that state.

#This class "State" is a generic superclass from which all the other
#states will inherit  


class State(object):	
	def __init__(self,Data):
		self.Data = Data
		self.CommandMatcher = {
		"00" : "toIdle",
		"01" : "toMove",
		"02" : "toMove",
		"03" : "toMove",
		"04" : "toMove",
		"05" : "toExit",
	}				
	def EnteringState(self):
		pass
		
	def Execute (self):
		pass
		
	def ExitingState(self):
		pass
	
	def CheckForCommand(self):
		txt = open("control/Commands.txt")
		value = txt.read()
		val = value.split('-', 1)
		self.RawCommand = val[0]
		self.speed = int(val[1])
		self.FCollistion = False
		self.BCollistion = False
		self.Command = self.CommandMatcher[self.RawCommand]
		
		if self.RawCommand == "01":
			if hardware_controller.irFront():
				self.Command = "toCollision"			
		if self.RawCommand == "02":
			if hardware_controller.irBack():				
				self.Command = "toCollision"
		
		
class Start(State):
	def __init__(self, Data):
		super(Start, self).__init__(Data)
		
	def EnteringState(self):		
		pass
		
	def Execute(self):
		print("In start")
		with open('state.txt','w') as f:
			f.write("Started")
		time.sleep(1)
		if os.path.isfile('control/Commands.txt'):
			pass
		else:
			open('control/Commands.txt',"w").close()		
		self.Data.next_Transition("toIdle")
			
	def ExitingState(self):
		with open('state.txt','w') as f:
			f.write("Loading")

class Idle(State):
	def __init__(self, Data):
		super(Idle, self).__init__(Data)
		
	def EnteringState(self): 
		with open('state.txt','w') as f:
			f.write("Idle")
	
	def Execute(self):
		print("In Idle")
		super(Idle , self).CheckForCommand()
		hardware_controller.stop()
		if self.Command == "toIdle":
			pass
		else:
			self.Data.next_Transition(self.Command)
			
	def ExitingState(self):
		pass
		
class Move(State):
	def __init__(self, Data):
		super(Move, self).__init__(Data)
		
	def EnteringState(self): 
		with open('state.txt','w') as f:
			f.write("Moving")
	
	def Execute(self):
		print("In Move")
		super(Move , self).CheckForCommand()
		print(self.Command)
		
		if self.RawCommand == "01":
			hardware_controller.forward(self.speed)
		if self.RawCommand == "02":
			hardware_controller.reverse(self.speed)
		if self.RawCommand == "03":
			hardware_controller.turnLeft(self.speed)
		if self.RawCommand == "04":
			hardware_controller.turnRight(self.speed)
		
		self.Data.next_Transition(self.Command)		
			
	def ExitingState(self):
		pass

class Collision(State):
	def __init__(self, Data):
		super(Collision, self).__init__(Data)
		
	def EnteringState(self): 
		with open('state.txt','w') as f:
			f.write("Path Blocked")
	
	def Execute(self):
		super(Collision , self).CheckForCommand()
		hardware_controller.stop()
		
		if self.RawCommand == "01":
			if not hardware_controller.irFront():
				hardware_controller.forward(self.speed)
		if self.RawCommand == "02":
			if not hardware_controller.irBack():
				hardware_controller.reverse(self.speed)
		if self.RawCommand == "03":
			hardware_controller.turnLeft(self.speed)
		if self.RawCommand == "04":
			hardware_controller.turnRight(self.speed)		
			
		#print(self.Command)
		self.Data.next_Transition(self.Command)		
			
	def ExitingState(self):
		pass

class Exit(State):
	def __init__(self, Data):
		super(Exit, self).__init__(Data)
		
	def EnteringState(self):
		print("In Exit")
		with open('state.txt','w') as f:
			f.write("Exit")
	
	def Execute(self):
		hardware_controller.cleanup()
		sys.exit(0)
			
	def ExitingState(self):
		pass
		

#######################################################################
##Data
#This section contains the class in which all the data will be kept
#such as all the transitions, states, current state ect.
#this class also has the def's which allow you to add Transitions 
#and states
class Data(object):
	def __init__(self):
		#self.char = character
		self.states = {}
		self.transitions = {}
		self.curState = None
		self.prevState = None
		self.trans = None
		
	def add_Transition(self,transName, transition):
		self.transitions[transName] = transition
		
	def add_State(self,stateName,state):
		self.states[stateName] = state
		
	def SetState(self, stateName):
		self.prevState = self.curState
		self.curState = self.states[stateName]
		
	def next_Transition(self, toTrans):
		self.trans = self.transitions[toTrans]
		
	def Execute(self):
		if(self.trans):
			self.curState.ExitingState()
			self.SetState(self.trans.toState)
			self.curState.EnteringState()
			self.trans = None			
		self.curState.Execute()
#######################################################################
## Runup code
if __name__=='__main__':
	Data = Data()
	##States
	Data.add_State("Start",Start(Data))
	Data.add_State("Idle",Idle(Data))
	Data.add_State("Move",Move(Data))
	Data.add_State("Collision",Collision(Data))
	Data.add_State("Exit",Exit(Data))

	##Transistion
	Data.add_Transition("toIdle", Transition("Idle"))
	Data.add_Transition("toMove", Transition("Move"))
	Data.add_Transition("toCollision", Transition("Collision"))
	Data.add_Transition("toExit", Transition("Exit"))
		
	Data.SetState("Start")
	while True:
		time.sleep(0.3)
		Data.Execute()
