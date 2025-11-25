window.MonopolyDialogue = (function(){
  let dotnetRef; let keyHandler; let clickHandler;
  function init(ref){
    dotnetRef = ref;
    keyHandler = (e)=>{
      if(!dotnetRef) return; dotnetRef.invokeMethodAsync('OnAdvanceRequestedAsync');
    };
    clickHandler = (e)=>{
      if(!dotnetRef) return; dotnetRef.invokeMethodAsync('OnAdvanceRequestedAsync');
    };
    window.addEventListener('keydown', keyHandler);
    document.addEventListener('click', clickHandler);
  }
  function dispose(){
    window.removeEventListener('keydown', keyHandler);
    document.removeEventListener('click', clickHandler);
    dotnetRef = null;
  }
  return { init, dispose };
})();
