   M S B A M L   `   `   `  ����   
crosshair3    crosshair3.MainWindow   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml       � Title$   SwitchScope Crosshair V3.1��$	��660��$	��800�� � WindowStartupLocation$ CenterScreen=�.��� .+��� $��20q�.+��� $7��1SwitchScope Crosshair V3.1 Founder Deluxe Edition��$
��Bold"�$��14(�$��0,0,0,20q�$��Center=��� $��Current Crosshair Status��$��0,0,0,20q�.��� $��5q� � ColumnDefinitions �� $
��Auto ��� $��* � � RowDefinitions �� $
��Auto ��� $
��Auto �.+��� $��Current Crosshair: ��$��0��$��0��$��0,0,10,5q�$��Center=��� -    ��Name$ CurrentCrosshairText��$��0��$��1��$��0,0,0,5q�$��Center=���  �� Orientation$ 
Horizontal=�$��1��$��0��$��2��.+��� $��
Position: ��$��Center=�$��0,0,10,0q��� $��X:��$��Center=�$��0,0,5,0q��� -   $��-��$��20��$��0,0,5,0q��� -    ��Name$ PositionXTextBox��$��60��$��0,0,5,0q��� -   $��+��$��20��$��0,0,10,0q��� $��Y:��$��Center=�$��0,0,5,0q��� -   $��-��$��20��$��0,0,5,0q��� -   $ PositionYTextBox��$��60��$��0,0,5,0q��� -   $��+��$��20��$��0,0,10,0q��� $��Crosshair List��$��0,0,0,20q�.���� -    ��Name$ CrosshairListBox��$	��100��$��5q��� $ 
Horizontal=�$��0,0,0,20q�.+��� -	   $��Select Crosshair 1��$��0,0,10,0q�$
��10,5q��� -
   $��Select Crosshair 2��$��0,0,10,0q�$
��10,5q��� -   $��Select Crosshair 3��$��0,0,10,0q�$
��10,5q��� $ 
Horizontal=�$��0,0,0,20q�.+��� -   $��Center Crosshair��$��0,0,10,0q�$
��10,5q��� $��Hotkey Settings��$��0,0,0,20q�.���� $��5q�.+��� $��Current Switch Hotkey:��$��0,0,0,5q��� $ 
Horizontal=�.+��� -   $ HotkeyTextBox��$	��100�� �� 
IsReadOnly$
 True��$��0��$��0,0,10,0q��� -   $��Set Switch Hotkey��$
��10,5q��� $��Default Crosshair Folder��$��0,0,0,20q�.���� $��5q�.+��� $��Current Default Folder:��$��0,0,0,5q��� -   $ DefaultFolderTextBox��$
 True��$��0,0,0,10q�$��Documents\Crosshairs���� $ 
Horizontal=�.+��� -   $��Change Default Folder��$��0,0,10,0q�$
��10,5q��� -   $��Open Default Folder��$
��10,5q��� $ 
Horizontal=�$��0,0,0,10q�.+��� $��Size: ��$��Center=�$��0,0,10,0q��� -   	 ��Name$	 
SizeSlider��$	��200��
 � Minimum$
 50Q� � Maximum$	 400Q� � Value$	 100Q��� $��Center=�$��10,0,0,0q�����  �� ElementName$ 
SizeSlider�� �� Path$ Value� �� StringFormat$ {0:F0}%���� -   $��Switch Test��$��0,0,0,10q�$
��10,5q�$
��Left=��� -    ��Name$ ActivateButton��$��Activate��$��0,0,10,0q�$
��10,5q�
��� ���$
��Bold"�$��14(�