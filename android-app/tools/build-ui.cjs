const fs=require('fs'),path=require('path');
const babel=require('@babel/core');
const root=path.resolve(__dirname,'..'),input=path.join(root,'app/src/main/assets/ui.source.html');
let html=fs.readFileSync(input,'utf8');
const polyfills=`
if(!Object.entries)Object.entries=function(o){return Object.keys(o).map(function(k){return [k,o[k]];});};
if(!Object.values)Object.values=function(o){return Object.keys(o).map(function(k){return o[k];});};
if(!Array.prototype.flatMap)Array.prototype.flatMap=function(fn){var out=[];for(var i=0;i<this.length;i++)out=out.concat(fn(this[i],i,this));return out;};
if(!Array.prototype.at)Array.prototype.at=function(i){return this[i<0?this.length+i:i];};
if(!NodeList.prototype.forEach)NodeList.prototype.forEach=Array.prototype.forEach;
if(!Element.prototype.append)Element.prototype.append=function(){for(var i=0;i<arguments.length;i++)this.appendChild(typeof arguments[i]==='string'?document.createTextNode(arguments[i]):arguments[i]);};
if(!Element.prototype.replaceChildren)Element.prototype.replaceChildren=function(){while(this.firstChild)this.removeChild(this.firstChild);this.append.apply(this,arguments);};
`;
html=html.replace(/<script>([\s\S]*?)<\/script>/,(_,script)=>{
 const mobileFallback=`
 if(!(window.CSS&&CSS.supports('display','grid'))){toggleSubGroup=function(id){var group=Array.from(document.querySelectorAll('.sub-group[data-sub-id]')).find(function(g){return g.dataset.subId===id;});if(!group)return;var list=group.querySelector('.sub-group-nodes-list');var open=group.classList.contains('open');if(open){list.style.height=list.firstElementChild.scrollHeight+'px';void list.offsetHeight;list.style.height='0px';group.classList.remove('open');closedGroups.add(id);}else{list.style.height=list.firstElementChild.scrollHeight+'px';group.classList.add('open');closedGroups.delete(id);}group.querySelector('.sub-group-header').setAttribute('aria-expanded',String(!open));};}
 `;
 const code=babel.transformSync(polyfills+script+'\n'+mobileFallback,{presets:[['@babel/preset-env',{targets:{chrome:'51'},modules:false}]],comments:false,compact:false}).code;
 return '<script>'+code+'</script>';
});
fs.writeFileSync(path.join(root,'app/src/main/assets/ui.html'),html);
