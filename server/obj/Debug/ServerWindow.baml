   M S B A M L   `   `   `  ����  	ServerWPF    ServerWPF.ServerWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     6F   -   5          � 
ResizeMode$  CanMinimize=�5   	    � Title$ ServerWindow��5   	   $	��533��6   $	��394��6+    � WindowStartupLocation$ CenterOwner=�6Z   .��5      � $��0,0,0,3q�6   .+�5
   
   �� $��Configurazione Server��6   $
��Left=�62   $��
10,138,0,0q�6M   $	��Top=�6a   $	��361��6z   $	��351��6�   
�������.��5      � $��0,1,0,-1q�6    � 	Resources 5         Password_Visible��%y   &	        �� 5      $��/images/pass_visible.png��65   $��UniformToFill=�6W   $��Center=�6o   $��Center=�6�   5      .+�5      �� -   5         ��Name$ nameTextBox��5      $
��Left=�6/   $��22��6J   $��6,33,0,0q�6V    �� TextWrapping$
 Wrap=�6h   $��
��6|   $	��Top=�6�   $	��171��6�   6�   �� 5      -   5         ��Name$ 	nameLabel��5      $��Nome Server��6+   $
��Left=�6A   $��25��6\   $��6,8,0,0q�6h   $	��Top=�6y   $	��147��6�   �� 5      -   5        $ passwordLabel��5      $��Password��6/   $
��Left=�6B   $��25��6]   $��6,84,0,0q�6i   $	��Top=�6{   $	��147��6�   �� 5      -   5        $ addressLabel��5      $��	Indirizzo��6.   $
��Left=�6B   $��25��6]   $��	6,149,0,0q�6i   $	��Top=�6|   $	��147��6�   �� 5      -   5        $ portTextBox��5   4   $
��Left=�6I   $��21��6d   $��	6,243,0,0q�6p   $
 Wrap=�6�   $	��Top=�6�   $	��171��6�   6�   �� 5      -   5        $ 	portLabel��5      $��	Porta TCP��6+   $
��Left=�6?   $��25��6Z   $��	6,217,0,0q�6f   $	��Top=�6y   $	��147��6�   �� 5      -   5         ��Name$ applyButton��5      $��Applica��6.   $
��Left=�6@   $��25��6[   $��
239,92,0,0q�6g   $	��Top=�6{   $��90��6�   6�   �� 5      -	   5        $ modificaButton��5      $��Modifica��61   $
��Left=�6D   $��24��6_   $��239,143,0,0q�6k   $	��Top=�6�   $��90��6�   6�   G� 5      -
   5         G�Name$ passwordTextBox��5   ^   $
��Left=�6w   $��	6,109,0,0q�6�   $	��Top=�6�   $	��171��6�   $��22��6�   6�   �� 5      -   5        $ passwordVisibleTextBox��5      $
��Left=�6:   $��	6,109,0,0q�6U   $	��Top=�6h   $y�Hidden=�6�   $	��171��6�   $��22��6�   �� 5      -   5        $ ripristinaButton��5      $��Annulla��63   $
��Left=�6E   $��24��6`   $��239,194,0,0q�6l   $	��Top=�6�   $��90��6�   6�   �� 5      -   5        	 ��Name$	 addressTextBox��5      $
��Left=�63   $��	6,174,0,0q�6N   $	��Top=�6a   $	��171��6y   6�   �� 5      -   5        $ portLabelUDP��5      $��	Porta UDP��6.   $
��Left=�6B   $��25��6]   $��	6,273,0,0q�6i   $	��Top=�6|   $	��147��6�   �� 5      -   5        $ portTextBoxUDP��5   4   $
��Left=�6L   $��21��6g   $��	6,298,0,0q�6s   $
 Wrap=�6�   $	��Top=�6�   $	��171��6�   6�   �� 5      -   5        
 ��Name$
 checkBoxEnableUdp��5      $�� ��66   $
��Left=�6A   $��158,278,0,0q�6\   $	��Top=�6q   6�   �� 5      -   5        $
 checkBoxPassword��5      $�� ��65   $
��Left=�6@   $��
158,89,0,0q�6[   $	��Top=�6o   6�   �� 5       -   5         ��Name$ passwordVisibleButton��5       $��/images/pass_visible.png��67   $��UniformToFill=�6Y   $��Center=�6q   $��Center=�6�   $��156,111,176,202q�6�   $y�Hidden=�6Z  $��16��6n  $��17��6z  5!      5"      �� 5#   
   $��Stato Server��6   $
��Left=�6)   $��10,2,0,0q�6D   $	��Top=�6V   $	��361��6n   $	��130��6z   
�������.��5$      � .+�5%      �� -   5        $��startButton��5%      $��Start��6-   $
��Left=�6=   $��
232,17,0,0q�6X   $	��Top=�6l   $��97��6�   6�   �� 5&      -   5        $��
stopButton��5&      $
��Stop��6+   $
��Left=�6:   $��
232,66,0,0q�6U   $	��Top=�6i   $��97��6�   6�   �� 5'      $��Stato: ��6   $
��Left=�6+   $��	10,17,0,0q�6F   $	��Top=�6Y    J� RenderTransformOrigin$ -0.132,0.115&�6q   $��41��6�   �� 5(      $��
Attivo da:��6   $
��Left=�6-   $��	10,61,0,0q�6H   $	��Top=�6[   �� 5)      -   5        $��
stateLabel��5)      $�� ��6*   $
��Left=�65   $��	69,17,0,0q�6P   $	��Top=�6c   $	��115��6{   �� 5*      -   5        $ timer��5*      $�� ��6'   $
��Left=�62   $��	76,63,0,0q�6M   $	��Top=�6`   $��76��6x   $��25��6�   5,      5.      50      51      