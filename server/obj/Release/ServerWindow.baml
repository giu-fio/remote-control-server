   M S B A M L   `   `   `  ����   	ServerWPF    ServerWPF.ServerWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     -     � 
ResizeMode$  CanMinimize=� � Title$ ServerWindow��$	��533��$	��394�� � WindowStartupLocation$ CenterOwner=�.��� $��0,0,0,3q�.+��� $��Configurazione Server��$
��Left=�$��
10,138,0,0q�$	��Top=�$	��361��$	��351��
�������.��� $��0,1,0,-1q� � 	Resources    Password_Visible��%\   &	        �� $��/images/pass_visible.png��$��UniformToFill=�$��Center=�$��Center=�.+��� -    ��Name$ nameTextBox��$
��Left=�$��22��$��6,33,0,0q� �� TextWrapping$
 Wrap=�$��
��$	��Top=�$	��171���� -    ��Name$ 	nameLabel��$��Nome Server��$
��Left=�$��25��$��6,8,0,0q�$	��Top=�$	��147���� -   $ passwordLabel��$��Password��$
��Left=�$��25��$��6,84,0,0q�$	��Top=�$	��147���� -   $ addressLabel��$��	Indirizzo��$
��Left=�$��25��$��	6,149,0,0q�$	��Top=�$	��147���� -   $ portTextBox��$
��Left=�$��21��$��	6,243,0,0q�$
 Wrap=�$	��Top=�$	��171���� -   $ 	portLabel��$��	Porta TCP��$
��Left=�$��25��$��	6,217,0,0q�$	��Top=�$	��147���� -    ��Name$ applyButton��$��Applica��$
��Left=�$��25��$��
239,92,0,0q�$	��Top=�$��90���� -	   $ modificaButton��$��Modifica��$
��Left=�$��24��$��239,143,0,0q�$	��Top=�$��90��G� -
    G�Name$ passwordTextBox��$
��Left=�$��	6,109,0,0q�$	��Top=�$	��171��$��22���� -   $ passwordVisibleTextBox��$
��Left=�$��	6,109,0,0q�$	��Top=�$y�Hidden=�$	��171��$��22���� -   $ ripristinaButton��$��Annulla��$
��Left=�$��24��$��239,194,0,0q�$	��Top=�$��90���� -   	 ��Name$	 addressTextBox��$
��Left=�$��	6,174,0,0q�$	��Top=�$	��171���� -   $ portLabelUDP��$��	Porta UDP��$
��Left=�$��25��$��	6,273,0,0q�$	��Top=�$	��147���� -   $ portTextBoxUDP��$
��Left=�$��21��$��	6,298,0,0q�$
 Wrap=�$	��Top=�$	��171���� -   
 ��Name$
 checkBoxEnableUdp��$�� ��$
��Left=�$��158,278,0,0q�$	��Top=��� -   $
 checkBoxPassword��$�� ��$
��Left=�$��
158,89,0,0q�$	��Top=��� -    ��Name$ passwordVisibleButton��$��/images/pass_visible.png��$��UniformToFill=�$��Center=�$��Center=�$��156,111,176,202q�$y�Hidden=�$��16��$��17���� $��Stato Server��$
��Left=�$��10,2,0,0q�$	��Top=�$	��361��$	��130��
�������.��� .+��� -   $��startButton��$��Start��$
��Left=�$��
232,17,0,0q�$	��Top=�$��97���� -   $��
stopButton��$
��Stop��$
��Left=�$��
232,66,0,0q�$	��Top=�$��97���� $��Stato: ��$
��Left=�$��	10,17,0,0q�$	��Top=� J� RenderTransformOrigin$ -0.132,0.115&�$��41���� $��
Attivo da:��$
��Left=�$��	10,61,0,0q�$	��Top=��� -   $��
stateLabel��$�� ��$
��Left=�$��	69,17,0,0q�$	��Top=�$	��115���� -   $ timer��$�� ��$
��Left=�$��	76,63,0,0q�$	��Top=�$��76��$��25��